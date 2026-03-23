using System.Collections.Generic;
using UnityEngine;

public class SyncedCommandManager : MonoBehaviour
{
    [Header("References")]
    public CommsDataProvider comms;
        
    [Header("Gameplay References")]
    public FireChallengeManager fireChallengeManager;
    public RiotShieldController riotShieldController;

    [Header("Sync Settings")]
    public float syncWindowSeconds = 3f;

    [Header("Debug")]
    public bool verboseLogs = true;
    public bool logIgnoredInputs = true;
    public bool logTimeouts = true;
    public bool logQueueing = true;

    [Header("Gameplay Targets")]
    public ExtinguisherExtinguish_CameraRay extinguisher; // fallback
    public ExtinguisherModelSwitcher modelSwitcher;
    public DoorBreachUIController doorBreachUIController;



    private enum Source
    {
        Flex,
        Imu,
        Audio
    }

    private enum Command
    {
        None,
        Normal,
        Pressure,
        Breach,
        Block,
        PullPin,
        Carbon,
        Chemical,
        Dioxide,
        Foam,
        Next,
        Powder,
        Previous,
        Water
    }

    private enum SprayMode
    {
        None,
        Normal,
        Pressure
    }

    private class SyncState
    {
        public bool active;
        public float deadline;
        public bool gotImu;
        public bool gotAudio;
    }

    private struct PendingInput
    {
        public Source source;
        public int value;

        public PendingInput(Source source, int value)
        {
            this.source = source;
            this.value = value;
        }
    }

    private readonly object locker = new object();
    private readonly Queue<PendingInput> pendingInputs = new Queue<PendingInput>();
    private readonly Dictionary<Command, SyncState> syncStates = new Dictionary<Command, SyncState>();

    private SprayMode currentSprayMode = SprayMode.Normal;

    private void Start()
    {
        comms = CommsDataProvider.Instance;

        if (comms == null)
        {
            Debug.LogError("[SyncManager] CommsDataProvider.Instance not found.");
            enabled = false;
            return;
        }

        if (doorBreachUIController == null)
        doorBreachUIController = FindObjectOfType<DoorBreachUIController>();

        if (doorBreachUIController == null)
        Debug.LogWarning("[SyncManager] No DoorBreachUIController found in scene.");

        if (modelSwitcher == null)
            modelSwitcher = FindObjectOfType<ExtinguisherModelSwitcher>();

        if (modelSwitcher == null)
            Debug.LogWarning("[SyncManager] No ExtinguisherModelSwitcher found in scene.");

        if (extinguisher == null)
            extinguisher = FindObjectOfType<ExtinguisherExtinguish_CameraRay>();

        if (extinguisher == null)
            Debug.LogWarning("[SyncManager] No fallback ExtinguisherExtinguish_CameraRay found in scene.");

        Debug.Log($"[SyncManager] Using comms instance id={comms.GetInstanceID()} on object={comms.gameObject.name}");
        Debug.Log($"[SyncManager] comms == CommsDataProvider.Instance ? {comms == CommsDataProvider.Instance}");

        syncStates[Command.Normal] = new SyncState();
        syncStates[Command.Pressure] = new SyncState();
        syncStates[Command.Breach] = new SyncState();
        syncStates[Command.Block] = new SyncState();

        comms.OnFlexUpdated += OnFlexUpdated;
        comms.OnImuUpdated += OnImuUpdated;
        comms.OnProcessAudioUpdated += OnAudioUpdated;

        Log("Started. Listening for Flex / IMU / Audio updates.");
        Log("Synced rules:");
        Log("  Normal   = IMU3 + Audio7 -> sets NORMAL mode");
        Log("  Pressure = IMU4 + Audio9 -> sets PRESSURE mode");
        Log("  Breach   = IMU2 + Audio1");
        Log("  Block    = IMU5 + Audio0");
        Log("Immediate rules:");
        Log("  PullPin  = IMU1");
        Log("  Foam     -> switch to extinguisher index 0");
        Log("  Water    -> switch to extinguisher index 1");
        Log("  Powder   -> switch to extinguisher index 2");
        Log("  Carbon   -> switch to extinguisher index 3");
        Log("  Chemical -> switch to extinguisher index 4");
        Log("  Next / Previous -> increment / decrement index");
        Log("Flex rules:");
        Log("  FLEX0 = release");
        Log("  FLEX1 = normal flex");
        Log("  FLEX2 = pressure flex");
    }

    private void OnDestroy()
    {
        if (comms != null)
        {
            comms.OnFlexUpdated -= OnFlexUpdated;
            comms.OnImuUpdated -= OnImuUpdated;
            comms.OnProcessAudioUpdated -= OnAudioUpdated;
        }
    }

    private ExtinguisherExtinguish_CameraRay GetActiveExtinguisher()
    {
        if (modelSwitcher != null)
        {
            ExtinguisherExtinguish_CameraRay current = modelSwitcher.GetCurrentExtinguisher();
            if (current != null)
                return current;
        }

        return extinguisher;
    }

    private void OnFlexUpdated(int value)
    {
        Debug.Log($"[SyncManager] OnFlexUpdated({value})");

        lock (locker)
        {
            pendingInputs.Enqueue(new PendingInput(Source.Flex, value));
        }

        if (logQueueing)
            Debug.Log($"[SyncManager] Queued FLEX={value} ({DescribeFlex(value)})");
    }

    private void OnImuUpdated(int value)
    {
        Debug.Log($"[SyncManager] OnImuUpdated({value})");

        lock (locker)
        {
            pendingInputs.Enqueue(new PendingInput(Source.Imu, value));
        }

        if (logQueueing)
            Debug.Log($"[SyncManager] Queued IMU={value} ({DescribeImu(value)})");
    }

    private void OnAudioUpdated(int value)
    {
        Debug.Log($"[SyncManager] OnAudioUpdated({value})");

        lock (locker)
        {
            pendingInputs.Enqueue(new PendingInput(Source.Audio, value));
        }

        if (logQueueing)
            Debug.Log($"[SyncManager] Queued AUDIO={value} ({DescribeAudio(value)})");
    }

    private void Update()
    {
        if (pendingInputs.Count > 0)
            Debug.Log($"[SyncManager] Update() sees {pendingInputs.Count} pending inputs");

        ExpireTimedOutSyncs();

        while (true)
        {
            PendingInput input;

            lock (locker)
            {
                if (pendingInputs.Count == 0)
                    break;

                input = pendingInputs.Dequeue();
            }

            Debug.Log($"[SyncManager] Dequeued {input.source}={input.value}");
            ProcessInput(input.source, input.value);
        }
    }

    private void ExpireTimedOutSyncs()
    {
        float now = Time.time;

        foreach (var kvp in syncStates)
        {
            Command command = kvp.Key;
            SyncState state = kvp.Value;

            if (state.active && now > state.deadline)
            {
                if (logTimeouts)
                {
                    Debug.Log(
                        $"[SyncManager] TIMEOUT: {command} expired after {syncWindowSeconds:F1}s. " +
                        $"State before reset = {DescribeState(command, state)}"
                    );
                }

                ResetState(state);
            }
        }
    }

    private void ProcessInput(Source source, int value)
    {
        if (source == Source.Flex)
        {
            HandleFlexInput(value);
            return;
        }

        string rawDesc = DescribeRawInput(source, value);
        Log($"RAW INPUT -> {rawDesc}");

        Command command = MapInputToCommand(source, value);

        if (command == Command.None)
        {
            if (logIgnoredInputs)
                Debug.Log($"[SyncManager] IGNORE: {rawDesc} does not map to any command used by this manager.");
            return;
        }

        bool synced = IsSyncedCommand(command);

        Log($"MAPPED: {rawDesc} -> Command={command} ({(synced ? "SYNCED" : "IMMEDIATE")})");

        if (synced)
            HandleSyncedCommand(source, command, value);
        else
            TriggerImmediateCommand(command, source, value);
    }

    private void HandleFlexInput(int value)
    {
        ExtinguisherExtinguish_CameraRay activeExtinguisher = GetActiveExtinguisher();

        Debug.Log($"[SyncManager] FLEX INPUT -> FLEX={value} ({DescribeFlex(value)}), currentMode={currentSprayMode}");

        if (activeExtinguisher == null)
        {
            Debug.LogWarning("[SyncManager] FLEX ignored because active extinguisher reference is missing.");
            return;
        }

        if (value == 0)
        {
            activeExtinguisher.SetCommsSprayHeld(false);
            Debug.Log("[SyncManager] FLEX RELEASE -> spray OFF");
            return;
        }

        if (value == 1)
        {
            if (currentSprayMode == SprayMode.Normal)
            {
                activeExtinguisher.SetCommsSprayHeld(true);
                Debug.Log("[SyncManager] FLEX NORMAL accepted in NORMAL mode -> spray ON");
            }
            else
            {
                activeExtinguisher.SetCommsSprayHeld(false);
                Debug.Log($"[SyncManager] FLEX NORMAL ignored because current mode is {currentSprayMode}");
            }

            return;
        }

        if (value == 2)
        {
            if (currentSprayMode == SprayMode.Pressure)
            {
                activeExtinguisher.SetCommsSprayHeld(true);
                Debug.Log("[SyncManager] FLEX PRESSURE accepted in PRESSURE mode -> spray ON");
            }
            else
            {
                activeExtinguisher.SetCommsSprayHeld(false);
                Debug.Log($"[SyncManager] FLEX PRESSURE ignored because current mode is {currentSprayMode}");
            }

            return;
        }

        Debug.Log($"[SyncManager] FLEX value {value} is unknown -> spray OFF");
        activeExtinguisher.SetCommsSprayHeld(false);
    }

    private Command MapInputToCommand(Source source, int value)
    {
        switch (source)
        {
            case Source.Flex:
                return Command.None;

            case Source.Imu:
                if (value == 1) return Command.PullPin;
                if (value == 2) return Command.Breach;
                if (value == 3) return Command.Normal;
                if (value == 4) return Command.Pressure;
                if (value == 5) return Command.Block;
                return Command.None;

            case Source.Audio:
                if (value == 0) return Command.Block;
                if (value == 1) return Command.Breach;
                if (value == 2) return Command.Carbon;
                if (value == 3) return Command.Chemical;
                if (value == 4) return Command.Dioxide;
                if (value == 5) return Command.Foam;
                if (value == 6) return Command.Next;
                if (value == 7) return Command.Normal;
                if (value == 8) return Command.Powder;
                if (value == 9) return Command.Pressure;
                if (value == 10) return Command.Previous;
                if (value == 11) return Command.Water;
                return Command.None;

            default:
                return Command.None;
        }
    }

    private bool IsSyncedCommand(Command command)
    {
        return command == Command.Normal ||
               command == Command.Pressure ||
               command == Command.Breach ||
               command == Command.Block;
    }

    private void HandleSyncedCommand(Source source, Command command, int rawValue)
    {
        CancelConflictingStates(source, command);

        SyncState state = syncStates[command];

        if (!state.active)
        {
            state.active = true;
            state.deadline = Time.time + syncWindowSeconds;
            state.gotImu = false;
            state.gotAudio = false;

            Debug.Log(
                $"[SyncManager] START WINDOW: {command} started by {DescribeRawInput(source, rawValue)}. " +
                $"Deadline in {syncWindowSeconds:F1}s at t={state.deadline:F2}"
            );
        }
        else
        {
            Debug.Log(
                $"[SyncManager] WINDOW ALREADY ACTIVE: {command}. " +
                $"Received additional input {DescribeRawInput(source, rawValue)}. " +
                $"Current state before mark = {DescribeState(command, state)}"
            );
        }

        MarkSourceReceived(state, source);

        Debug.Log($"[SyncManager] UPDATED STATE: {DescribeState(command, state)}");

        if (IsComplete(state))
        {
            Debug.Log(
                $"[SyncManager] SYNC SUCCESS: {command} completed by {DescribeRawInput(source, rawValue)} " +
                $"within {syncWindowSeconds:F1}s."
            );

            TriggerSyncedCommand(command);
            ResetState(state);
        }
        else
        {
            Debug.Log(
                $"[SyncManager] WAITING: {command} is not complete yet. " +
                $"Need partner before t={state.deadline:F2}"
            );
        }
    }

    private void CancelConflictingStates(Source incomingSource, Command keepCommand)
    {
        foreach (var kvp in syncStates)
        {
            Command cmd = kvp.Key;
            SyncState state = kvp.Value;

            if (cmd == keepCommand || !state.active)
                continue;

            bool conflict = UsesSource(cmd, incomingSource);

            if (conflict)
            {
                Debug.Log(
                    $"[SyncManager] CANCEL: Incoming source {incomingSource} for {keepCommand} " +
                    $"cancels pending {cmd}. Previous state = {DescribeState(cmd, state)}"
                );

                ResetState(state);
            }
        }
    }

    private bool UsesSource(Command command, Source source)
    {
        switch (command)
        {
            case Command.Normal:
            case Command.Pressure:
            case Command.Breach:
            case Command.Block:
                return source == Source.Imu || source == Source.Audio;

            default:
                return false;
        }
    }

    private void MarkSourceReceived(SyncState state, Source source)
    {
        if (source == Source.Imu)
        {
            if (state.gotImu)
                Log("Duplicate IMU received for active sync window.");
            state.gotImu = true;
        }

        if (source == Source.Audio)
        {
            if (state.gotAudio)
                Log("Duplicate AUDIO received for active sync window.");
            state.gotAudio = true;
        }
    }

    private bool IsComplete(SyncState state)
    {
        return state.gotImu && state.gotAudio;
    }

    private void ResetState(SyncState state)
    {
        state.active = false;
        state.deadline = 0f;
        state.gotImu = false;
        state.gotAudio = false;
    }

    private void TriggerSyncedCommand(Command command)
    {
        ExtinguisherExtinguish_CameraRay activeExtinguisher = GetActiveExtinguisher();

        switch (command)
        {
            case Command.Normal:
                currentSprayMode = SprayMode.Normal;
                if (activeExtinguisher != null)
                    activeExtinguisher.SetCommsSprayHeld(false);
                Debug.Log("[SyncManager] >>> MODE SET: NORMAL (IMU 3 + Audio 7)");
                break;

            case Command.Pressure:
                currentSprayMode = SprayMode.Pressure;
                if (activeExtinguisher != null)
                    activeExtinguisher.SetCommsSprayHeld(false);
                Debug.Log("[SyncManager] >>> MODE SET: PRESSURE (IMU 4 + Audio 9)");
                break;

            case Command.Breach:
                Debug.Log("[SyncManager] >>> ACTION FIRED: BREACH (IMU 2 + Audio 1)");
                if (fireChallengeManager != null)
                {
                    fireChallengeManager.BreachDoorAndWin();
                }
                break;

            case Command.Block:
                Debug.Log("[SyncManager] >>> ACTION FIRED: BLOCK (IMU 5 + Audio 0)");

                if (riotShieldController != null)
                {
                    riotShieldController.TriggerBlockShield();
                }
                break;
        }
    }

    private void TriggerImmediateCommand(Command command, Source source, int rawValue)
    {
        Debug.Log($"[SyncManager] IMMEDIATE ACTION: {command} from {DescribeRawInput(source, rawValue)}");

        ExtinguisherExtinguish_CameraRay activeExtinguisher = GetActiveExtinguisher();

        switch (command)
        {
            case Command.PullPin:
                Debug.Log("[SyncManager] >>> ACTION FIRED: PULL PIN");
                if (activeExtinguisher != null)
                    activeExtinguisher.PullPinFromComms();
                break;

            case Command.Carbon:
                Debug.Log("[SyncManager] >>> ACTION FIRED: CARBON");
                if (modelSwitcher != null)
                    modelSwitcher.ShowCarbon();
                break;

            case Command.Chemical:
                Debug.Log("[SyncManager] >>> ACTION FIRED: CHEMICAL");
                if (modelSwitcher != null)
                    modelSwitcher.ShowChemical();
                break;

            case Command.Foam:
                Debug.Log("[SyncManager] >>> ACTION FIRED: FOAM");
                if (modelSwitcher != null)
                    modelSwitcher.ShowFoam();
                break;

            case Command.Next:
                Debug.Log("[SyncManager] >>> ACTION FIRED: NEXT");
                if (modelSwitcher != null)
                    modelSwitcher.NextModel();
                break;

            case Command.Powder:
                Debug.Log("[SyncManager] >>> ACTION FIRED: POWDER");
                if (modelSwitcher != null)
                    modelSwitcher.ShowPowder();
                break;

            case Command.Previous:
                Debug.Log("[SyncManager] >>> ACTION FIRED: PREVIOUS");
                if (modelSwitcher != null)
                    modelSwitcher.PreviousModel();
                break;

            case Command.Water:
                Debug.Log("[SyncManager] >>> ACTION FIRED: WATER");
                if (modelSwitcher != null)
                    modelSwitcher.ShowWater();
                break;

            case Command.Dioxide:
                Debug.Log("[SyncManager] >>> ACTION FIRED: DIOXIDE");
                if (modelSwitcher != null)
                    modelSwitcher.ShowCarbon();
                break;
        }
    }

    private string DescribeRawInput(Source source, int value)
    {
        switch (source)
        {
            case Source.Flex:
                return $"FLEX={value} ({DescribeFlex(value)})";
            case Source.Imu:
                return $"IMU={value} ({DescribeImu(value)})";
            case Source.Audio:
                return $"AUDIO={value} ({DescribeAudio(value)})";
            default:
                return $"{source}={value}";
        }
    }

    private string DescribeFlex(int value)
    {
        switch (value)
        {
            case 0: return "released";
            case 1: return "normal flex";
            case 2: return "pressure flex";
            default: return "unknown";
        }
    }

    private string DescribeImu(int value)
    {
        switch (value)
        {
            case 0: return "nothing / no audio";
            case 1: return "pull pin";
            case 2: return "breach";
            case 3: return "normal";
            case 4: return "pressure";
            case 5: return "block";
            default: return "unknown";
        }
    }

    private string DescribeAudio(int value)
    {
        switch (value)
        {
            case 0: return "block";
            case 1: return "breach";
            case 2: return "carbon";
            case 3: return "chemical";
            case 4: return "dioxide";
            case 5: return "foam";
            case 6: return "next";
            case 7: return "normal";
            case 8: return "powder";
            case 9: return "pressure";
            case 10: return "previous";
            case 11: return "water";
            default: return "unknown";
        }
    }

    private string DescribeState(Command command, SyncState state)
    {
        return $"{command} [active={state.active}, gotImu={state.gotImu}, gotAudio={state.gotAudio}, deadline={state.deadline:F2}]";
    }

    private void Log(string msg)
    {
        if (verboseLogs)
            Debug.Log("[SyncManager] " + msg);
    }
}