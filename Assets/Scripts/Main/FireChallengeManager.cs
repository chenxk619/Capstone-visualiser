using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;

public class FireChallengeManager : MonoBehaviour
{
    [Header("Challenge Rules")]
    public int firesToWin = 5;
    public float timeLimitSeconds = 60f;

    [Header("Cheat")]
    public bool cheatMode = false;
    public float cheatTimeSeconds = 99999f;

    [Header("Fire Groups (order: A, B, C, Electrical, F)")]
    public FireGroupController[] fireGroups;

    [Header("Special Fire Burst - Class C")]
    public int burstFireIndex = 2;
    public GameObject fireBurstObject;
    public Transform burstTargetCamera;
    public float burstBlockTimeLimit = 10f;
    public float burstStopDistance = 0.5f;

    [Header("Special Door Breach - Class F")]
    public int breachFireIndex = 4;
    public GameObject breachDoorObject;
    public DoorBreachController breachDoorController;
    public float revealFireDelayAfterBreach = 2f;

    [Header("Shield")]
    public RiotShieldController riotShieldController;

    [Header("References")]
    public ExtinguisherModelSwitcher extinguisherSwitcher;
    public GameUIManager uiManager;
    public UIDocument uiDocument;

    [Header("UI Toolkit")]
    public string timerBarName = "timerBar";
    public string fireExtinguishedLabelName = "FireExtinguished";
    public string fireNameLabelName = "FireName";

    private ProgressBar timerBar;
    private Label fireExtinguishedLabel;
    private Label fireNameLabel;

    private float timeLeft;
    private bool running = false;
    private int extinguishedCount = 0;
    private bool[] extinguishedFlags;

    private bool waitingForBurstBlock = false;
    private float burstTimer = 0f;
    private Vector3 burstStartPos;

    private bool classFDoorBreached = false;
    private bool waitingForClassFBreach = false;
    private Coroutine revealFireRoutine;

    public int CurrentFireIndex { get; private set; } = -1;
    public int ExtinguishedCount => extinguishedCount;
    public float TimeLeft => Mathf.Max(0f, timeLeft);
    public bool Running => running;

    void Start()
    {
        int count = (fireGroups != null && fireGroups.Length > 0) ? fireGroups.Length : 5;
        extinguishedFlags = new bool[count];

        if (uiDocument != null)
        {
            var root = uiDocument.rootVisualElement;
            timerBar = root.Q<ProgressBar>(timerBarName);
            fireExtinguishedLabel = root.Q<Label>(fireExtinguishedLabelName);
            fireNameLabel = root.Q<Label>(fireNameLabelName);

            if (timerBar != null)
            {
                timerBar.lowValue = 0f;
                timerBar.highValue = timeLimitSeconds;
            }
        }

        BindGroups();
        ResetChallenge();
        UpdateTimerUI();
        UpdateFireExtinguishedUI();
        UpdateFireNameUI("");
    }

    void Update()
    {
        if (!running) return;

        if (waitingForBurstBlock)
        {
            UpdateBurstAttack();
            return;
        }

        if (!cheatMode)
        {
            timeLeft -= Time.deltaTime;

            if (timeLeft <= 0f)
            {
                timeLeft = 0f;
                running = false;
                waitingForBurstBlock = false;
                waitingForClassFBreach = false;

                HideAllFires();
                HideFireBurst();
                HideBreachDoor();
                UpdateTimerUI();

                if (uiManager != null)
                    uiManager.ShowLose();

                Debug.Log("[Challenge] Time up. Lose.");
                return;
            }
        }
        else
        {
            timeLeft = cheatTimeSeconds;
        }

        UpdateTimerUI();
    }

    void BindGroups()
    {
        if (fireGroups == null) return;

        for (int i = 0; i < fireGroups.Length; i++)
        {
            if (fireGroups[i] == null) continue;
            fireGroups[i].fireIndex = i;
            fireGroups[i].challengeManager = this;
        }
    }

    public void StartChallenge()
    {
        cheatMode = false;
        ResetChallenge();
        running = true;

        UpdateTimerUI();
        UpdateFireExtinguishedUI();
        UpdateFireNameUI("");

        Debug.Log("[Challenge] Started. Waiting for scanned fire target.");
    }

    public void StartCheatMode()
    {
        cheatMode = true;
        ResetChallenge();
        running = true;
        timeLeft = cheatTimeSeconds;

        UpdateTimerUI();
        UpdateFireExtinguishedUI();
        UpdateFireNameUI("CHEAT MODE");

        Debug.Log("[Challenge] Cheat mode started. Waiting for scanned fire target.");
    }

    void ResetChallenge()
    {
        extinguishedCount = 0;
        timeLeft = cheatMode ? cheatTimeSeconds : timeLimitSeconds;
        running = false;

        waitingForBurstBlock = false;
        burstTimer = 0f;

        classFDoorBreached = false;
        waitingForClassFBreach = false;

        if (revealFireRoutine != null)
        {
            StopCoroutine(revealFireRoutine);
            revealFireRoutine = null;
        }

        CurrentFireIndex = -1;

        if (extinguishedFlags == null || extinguishedFlags.Length != fireGroups.Length)
            extinguishedFlags = new bool[fireGroups.Length];

        for (int i = 0; i < extinguishedFlags.Length; i++)
            extinguishedFlags[i] = false;

        HideAllFires();
        HideFireBurst();
        HideBreachDoor();

        if (breachDoorController != null)
            breachDoorController.ResetBreach();

        var extinguisher = GetCurrentExtinguisherScript();
        if (extinguisher != null)
            extinguisher.ResetForNextFire(null, true);

        ResetAllTrackedTargets();

        UpdateTimerUI();
        UpdateFireExtinguishedUI();
        UpdateFireNameUI("");
    }

    public void SetTrackedFire(int fireIndex, GameObject fire)
    {
        if (!running) return;
        if (waitingForBurstBlock) return;
        if (fireIndex < 0 || fireIndex >= fireGroups.Length) return;

        if (IsFireAlreadyExtinguished(fireIndex))
        {
            if (fire != null)
                fire.SetActive(false);
            return;
        }

        CurrentFireIndex = fireIndex;

        if (fireIndex == breachFireIndex)
        {
            if (!classFDoorBreached)
            {
                waitingForClassFBreach = true;

                if (fire != null)
                    fire.SetActive(false);

                ShowBreachDoor();

                var extinguisherForDoor = GetCurrentExtinguisherScript();
                if (extinguisherForDoor != null)
                    extinguisherForDoor.ResetForNextFire(null, false);

                UpdateFireNameUI("Breach Door");
                Debug.Log("[Challenge] Class F tracked. Waiting for breach.");
                return;
            }
            else
            {
                HideBreachDoor();
            }
        }

        if (fireGroups[fireIndex] != null)
            fireGroups[fireIndex].ShowGroup();

        var extinguisher = GetCurrentExtinguisherScript();
        if (extinguisher != null)
            extinguisher.ResetForNextFire(null, false);

        UpdateFireNameUI(GetDisplayFireName(fireIndex));
        Debug.Log($"[Challenge] Tracking fire index {fireIndex}: {GetDisplayFireName(fireIndex)}");
    }

    public void ClearTrackedFire(int fireIndex)
    {
        if (CurrentFireIndex != fireIndex) return;

        CurrentFireIndex = -1;

        if (fireIndex == breachFireIndex && !classFDoorBreached)
        {
            waitingForClassFBreach = false;
            HideBreachDoor();
        }

        if (fireIndex >= 0 && fireIndex < fireGroups.Length && fireGroups[fireIndex] != null)
            fireGroups[fireIndex].HideGroup();

        var extinguisher = GetCurrentExtinguisherScript();
        if (extinguisher != null)
            extinguisher.ResetForNextFire(null, false);

        UpdateFireNameUI("");

        Debug.Log($"[Challenge] Lost tracking for fire index {fireIndex}");
    }

    public bool TrySprayMiniFire(MiniFireUnit miniFire, float sprayAmount)
    {
        if (!running || miniFire == null)
            return false;

        if (waitingForBurstBlock)
            return false;

        if (CurrentFireIndex < 0 || CurrentFireIndex >= fireGroups.Length)
            return false;

        if (IsFireAlreadyExtinguished(CurrentFireIndex))
            return false;

        FireGroupController group = fireGroups[CurrentFireIndex];
        if (group == null)
            return false;

        return group.TrySprayMiniFire(miniFire, sprayAmount);
    }

    public void OnFireGroupCompleted(int fireIndex, GameObject fireGroupObject)
    {
        if (!running) return;
        if (fireIndex < 0 || fireIndex >= extinguishedFlags.Length) return;
        if (IsFireAlreadyExtinguished(fireIndex)) return;

        extinguishedFlags[fireIndex] = true;
        extinguishedCount++;

        MarkTrackedTargetExtinguished(fireIndex);

        if (CurrentFireIndex == fireIndex)
            CurrentFireIndex = -1;

        UpdateFireExtinguishedUI();
        UpdateFireNameUI("");

        Debug.Log($"[Challenge] Fire group completed: {fireIndex}");

        if (fireIndex == burstFireIndex)
        {
            StartBurstBlockPhase(fireGroupObject.transform.position);
            return;
        }

        CheckForWinOrContinue();
    }

    void CheckForWinOrContinue()
    {
        if (extinguishedCount >= firesToWin)
        {
            running = false;
            HideAllFires();
            HideBreachDoor();
            HideFireBurst();
            UpdateFireNameUI("Completed");

            Debug.Log("[Challenge] All required fires extinguished. Win.");
            StartCoroutine(WinSequence());
            return;
        }

        Debug.Log("[Challenge] Waiting for next scanned fire target.");
    }

    public bool IsFireAlreadyExtinguished(int fireIndex)
    {
        if (extinguishedFlags == null) return false;
        if (fireIndex < 0 || fireIndex >= extinguishedFlags.Length) return false;
        return extinguishedFlags[fireIndex];
    }

    IEnumerator WinSequence()
    {
        yield return new WaitForSeconds(1.0f);

        if (uiManager != null)
            uiManager.ShowWin();
    }

    void StartBurstBlockPhase(Vector3 spawnPosition)
    {
        if (fireBurstObject == null || burstTargetCamera == null)
        {
            Debug.LogWarning("[Challenge] Fire burst object or target camera missing.");
            CheckForWinOrContinue();
            return;
        }

        waitingForBurstBlock = true;
        burstTimer = burstBlockTimeLimit;

        Vector3 dirToCamera = (burstTargetCamera.position - spawnPosition).normalized;
        fireBurstObject.transform.position = spawnPosition + dirToCamera * 0.2f;
        fireBurstObject.transform.LookAt(burstTargetCamera);

        burstStartPos = fireBurstObject.transform.position;
        ShowFireBurst();
        UpdateFireNameUI($"BLOCK! {Mathf.CeilToInt(burstTimer)}");

        Debug.Log("[Challenge] Class C extinguished -> burst triggered.");
    }

    void UpdateBurstAttack()
    {
        if (fireBurstObject == null || burstTargetCamera == null)
            return;

        burstTimer -= Time.deltaTime;

        float total = Mathf.Max(0.01f, burstBlockTimeLimit);
        float progress = Mathf.Clamp01(1f - (burstTimer / total));

        Vector3 targetPos = burstTargetCamera.position;
        Vector3 newPos = Vector3.Lerp(burstStartPos, targetPos, progress);
        fireBurstObject.transform.position = newPos;
        fireBurstObject.transform.LookAt(burstTargetCamera);

        UpdateFireNameUI($"BLOCK! {Mathf.CeilToInt(Mathf.Max(0f, burstTimer))}");

        float distToCamera = Vector3.Distance(fireBurstObject.transform.position, burstTargetCamera.position);

        if (distToCamera <= burstStopDistance || burstTimer <= 0f)
        {
            if (cheatMode)
            {
                waitingForBurstBlock = false;
                HideFireBurst();
                UpdateFireNameUI("CHEAT MODE");
                Debug.Log("[Challenge] Cheat mode ON - burst ignored.");
                CheckForWinOrContinue();
                return;
            }

            waitingForBurstBlock = false;
            HideFireBurst();
            running = false;
            UpdateFireNameUI("Burned");

            Debug.Log("[Challenge] Player failed to block. Lose.");

            if (uiManager != null)
                uiManager.ShowLose();
        }
    }

    public void ExecuteBlock()
    {
        if (!waitingForBurstBlock) return;

        waitingForBurstBlock = false;

        if (riotShieldController != null)
            riotShieldController.TriggerBlockShield();

        HideFireBurst();
        UpdateFireNameUI("");

        Debug.Log("[Challenge] Fire burst blocked.");
        CheckForWinOrContinue();
    }

    public void ExecuteBreach()
    {
        if (!running)
        {
            Debug.LogWarning("[Challenge] ExecuteBreach ignored: not running.");
            return;
        }

        if (!waitingForClassFBreach)
        {
            Debug.LogWarning("[Challenge] ExecuteBreach ignored: not waiting for breach.");
            return;
        }

        if (classFDoorBreached)
        {
            Debug.LogWarning("[Challenge] ExecuteBreach ignored: already breached.");
            return;
        }

        classFDoorBreached = true;
        waitingForClassFBreach = false;
        CurrentFireIndex = breachFireIndex;

        if (breachDoorController != null)
            breachDoorController.BreachDoor();
        else
            Debug.LogWarning("[Challenge] No breachDoorController assigned.");

        if (revealFireRoutine != null)
            StopCoroutine(revealFireRoutine);

        revealFireRoutine = StartCoroutine(RevealClassFFireAfterBreach());
    }

    IEnumerator RevealClassFFireAfterBreach()
    {
        yield return new WaitForSeconds(revealFireDelayAfterBreach);

        HideBreachDoor();

        if (breachFireIndex < 0 || breachFireIndex >= fireGroups.Length)
        {
            revealFireRoutine = null;
            yield break;
        }

        if (fireGroups[breachFireIndex] != null)
            fireGroups[breachFireIndex].ShowGroup();

        var extinguisher = GetCurrentExtinguisherScript();
        if (extinguisher != null)
            extinguisher.ResetForNextFire(null, false);

        UpdateFireNameUI(GetDisplayFireName(breachFireIndex));
        Debug.Log("[Challenge] Class F fire revealed.");

        revealFireRoutine = null;
    }

    public void RefreshCurrentExtinguisher()
    {
        var extinguisher = GetCurrentExtinguisherScript();
        if (extinguisher != null)
            extinguisher.ResetForNextFire(null, false);
    }

    ExtinguisherExtinguish_CameraRay GetCurrentExtinguisherScript()
    {
        if (extinguisherSwitcher == null)
            return null;

        return extinguisherSwitcher.GetCurrentExtinguisherScript();
    }

    void HideAllFires()
    {
        if (fireGroups == null) return;

        foreach (var g in fireGroups)
        {
            if (g != null)
                g.FullReset(true);
        }
    }

    void ShowBreachDoor()
    {
        if (breachDoorObject == null) return;

        breachDoorObject.SetActive(true);
        foreach (var r in breachDoorObject.GetComponentsInChildren<Renderer>(true))
            r.enabled = true;
    }

    void HideBreachDoor()
    {
        if (breachDoorObject == null) return;

        foreach (var r in breachDoorObject.GetComponentsInChildren<Renderer>(true))
            r.enabled = false;

        breachDoorObject.SetActive(false);
    }

    void ShowFireBurst()
    {
        if (fireBurstObject == null) return;

        fireBurstObject.SetActive(true);

        var particles = fireBurstObject.GetComponentsInChildren<ParticleSystem>(true);
        foreach (var ps in particles)
        {
            ps.Clear(true);
            ps.Play(true);
        }
    }

    void HideFireBurst()
    {
        if (fireBurstObject == null) return;

        var particles = fireBurstObject.GetComponentsInChildren<ParticleSystem>(true);
        foreach (var ps in particles)
        {
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        fireBurstObject.SetActive(false);
    }

    void UpdateTimerUI()
    {
        if (timerBar == null) return;

        int secs = Mathf.CeilToInt(TimeLeft);
        int mm = secs / 60;
        int ss = secs % 60;

        timerBar.highValue = cheatMode ? cheatTimeSeconds : timeLimitSeconds;
        timerBar.value = TimeLeft;
        timerBar.title = cheatMode ? "Timer: ∞" : $"Timer: {mm:00}:{ss:00}";
    }

    void UpdateFireExtinguishedUI()
    {
        if (fireExtinguishedLabel != null)
            fireExtinguishedLabel.text = $"Fires: {extinguishedCount}/{firesToWin}";
    }

    void UpdateFireNameUI(string text)
    {
        if (fireNameLabel != null)
            fireNameLabel.text = text;
    }

    string GetDisplayFireName(int index)
    {
        switch (index)
        {
            case 0: return "Class A Fire";
            case 1: return "Class B Fire";
            case 2: return "Class C Fire";
            case 3: return "Electrical Fire";
            case 4: return "Class F Fire";
            default: return $"Fire {index}";
        }
    }

    void MarkTrackedTargetExtinguished(int fireIndex)
    {
        TrackedFireTarget[] allTargets = FindObjectsOfType<TrackedFireTarget>(true);

        foreach (var target in allTargets)
        {
            if (target != null && target.fireIndex == fireIndex)
            {
                target.MarkExtinguished();
                break;
            }
        }
    }

    void ResetAllTrackedTargets()
    {
        TrackedFireTarget[] allTargets = FindObjectsOfType<TrackedFireTarget>(true);

        foreach (var target in allTargets)
        {
            if (target != null)
                target.ResetTrackedFire();
        }
    }

    public void ResetAndStart()
    {
        if (uiManager != null)
        {
            if (uiManager.losePanel != null) uiManager.losePanel.SetActive(false);
            if (uiManager.winPanel != null) uiManager.winPanel.SetActive(false);
        }

        StartChallenge();
    }
}