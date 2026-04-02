using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;

public class FireChallengeManager : MonoBehaviour
{
    [Header("Challenge Rules")]
    public int firesToWin = 5;
    public float timeLimitSeconds = 600f;

    [Header("Fires (order: A, B, C, Electrical, F)")]
    public GameObject[] fires;

    [Header("Special Fire Burst - Class C")]
    public int burstFireIndex = 2; // Class C
    public GameObject fireBurstObject;
    public Transform burstTargetCamera;
    public float burstBlockTimeLimit = 10f;
    public float burstStopDistance = 0.5f;

    [Header("Special Door Breach - Class F")]
    public int breachFireIndex = 4; // Class F
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

    private bool winTriggered = false;
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
        if (fires != null && fires.Length > 0)
            extinguishedFlags = new bool[fires.Length];
        else
            extinguishedFlags = new bool[5];

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

            if (fireNameLabel == null)
                Debug.LogWarning($"[FireChallengeManager] Label '{fireNameLabelName}' not found.");

            if (fireExtinguishedLabel == null)
                Debug.LogWarning($"[FireChallengeManager] Label '{fireExtinguishedLabelName}' not found.");
        }

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

        UpdateTimerUI();
    }

    public void StartChallenge()
    {
        ResetChallenge();

        running = true;

        UpdateTimerUI();
        UpdateFireExtinguishedUI();
        UpdateFireNameUI("");

        Debug.Log("[Challenge] Started. Waiting for scanned fire target.");
    }

    void ResetChallenge()
    {
        extinguishedCount = 0;
        timeLeft = timeLimitSeconds;
        running = false;
        winTriggered = false;

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

        int count = (fires != null && fires.Length > 0) ? fires.Length : 5;
        if (extinguishedFlags == null || extinguishedFlags.Length != count)
            extinguishedFlags = new bool[count];

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
        if (fireIndex < 0) return;

        if (IsFireAlreadyExtinguished(fireIndex))
        {
            if (fire != null)
                fire.SetActive(false);
            return;
        }

        CurrentFireIndex = fireIndex;

        // Special Class F logic: show door first, hide fire until breached
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

                Debug.Log("[Challenge] Class F target tracked. Waiting for breach.");
                return;
            }
            else
            {
                HideBreachDoor();

                if (fire != null)
                    fire.SetActive(true);
            }
        }

        var extinguisher = GetCurrentExtinguisherScript();
        if (extinguisher != null)
            extinguisher.ResetForNextFire(fire, false);

        UpdateFireNameUI(GetDisplayFireName(fireIndex, fire));

        Debug.Log($"[Challenge] Tracking fire index {fireIndex}: {GetDisplayFireName(fireIndex, fire)}");
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

        var extinguisher = GetCurrentExtinguisherScript();
        if (extinguisher != null)
            extinguisher.ResetForNextFire(null, false);

        UpdateFireNameUI("");

        Debug.Log($"[Challenge] Lost tracking for fire index {fireIndex}");
    }

    public void OnFireExtinguished(GameObject extinguishedFire)
    {
        if (!running) return;
        if (waitingForBurstBlock) return;
        if (extinguishedFire == null) return;

        int extinguishedFireIndex = GetFireIndex(extinguishedFire);
        if (extinguishedFireIndex < 0)
        {
            Debug.LogWarning($"[Challenge] Could not find fire index for object: {extinguishedFire.name}");
            return;
        }

        if (IsFireAlreadyExtinguished(extinguishedFireIndex))
            return;

        extinguishedFlags[extinguishedFireIndex] = true;
        extinguishedCount++;

        MarkTrackedTargetExtinguished(extinguishedFireIndex);

        UpdateFireExtinguishedUI();

        Debug.Log($"[Challenge] Fire extinguished count = {extinguishedCount}, fire index = {extinguishedFireIndex}, fire name = {extinguishedFire.name}");

        if (CurrentFireIndex == extinguishedFireIndex)
            CurrentFireIndex = -1;

        UpdateFireNameUI("");

        if (extinguishedFireIndex == burstFireIndex)
        {
            StartBurstBlockPhase(extinguishedFire.transform.position);
            return;
        }

        CheckForWinOrContinue();
    }

    void StartBurstBlockPhase(Vector3 spawnPosition)
    {
        if (fireBurstObject == null || burstTargetCamera == null)
        {
            Debug.LogWarning("[Challenge] Fire burst object or burst target camera is missing.");
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

        Debug.Log("[Challenge] Class C extinguished -> Fire burst triggered. 10 seconds to block.");
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

        if (burstTimer <= 0f || distToCamera <= burstStopDistance)
        {
            waitingForBurstBlock = false;
            HideFireBurst();
            running = false;

            UpdateFireNameUI("Burned");
            Debug.Log("[Challenge] Player failed to block the fire burst. Lose.");

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

        Debug.Log("[Challenge] Fire burst blocked successfully.");

        CheckForWinOrContinue();
    }

    public void ExecuteBreach()
    {
        Debug.Log($"[Challenge] ExecuteBreach called | running={running}, waitingForClassFBreach={waitingForClassFBreach}, classFDoorBreached={classFDoorBreached}, CurrentFireIndex={CurrentFireIndex}");

        if (!running)
        {
            Debug.LogWarning("[Challenge] ExecuteBreach ignored: challenge is not running.");
            return;
        }

        if (!waitingForClassFBreach)
        {
            Debug.LogWarning("[Challenge] ExecuteBreach ignored: not waiting for Class F breach.");
            return;
        }

        if (classFDoorBreached)
        {
            Debug.LogWarning("[Challenge] ExecuteBreach ignored: door already breached.");
            return;
        }

        classFDoorBreached = true;
        waitingForClassFBreach = false;
        CurrentFireIndex = breachFireIndex;

        Debug.Log("[Challenge] Class F breach triggered.");

        if (breachDoorController != null)
        {
            breachDoorController.BreachDoor();
        }
        else
        {
            Debug.LogWarning("[Challenge] No breachDoorController assigned.");
        }

        if (revealFireRoutine != null)
            StopCoroutine(revealFireRoutine);

        revealFireRoutine = StartCoroutine(RevealClassFFireAfterBreach());
    }

    IEnumerator RevealClassFFireAfterBreach()
    {
        yield return new WaitForSeconds(revealFireDelayAfterBreach);

        HideBreachDoor();

        if (fires == null || breachFireIndex < 0 || breachFireIndex >= fires.Length)
        {
            Debug.LogWarning("[Challenge] Class F fire index is invalid.");
            revealFireRoutine = null;
            yield break;
        }

        GameObject classFFire = fires[breachFireIndex];
        if (classFFire == null)
        {
            Debug.LogWarning("[Challenge] Class F fire reference is missing.");
            revealFireRoutine = null;
            yield break;
        }

        CurrentFireIndex = breachFireIndex;

        classFFire.SetActive(true);

        var extinguisher = GetCurrentExtinguisherScript();
        if (extinguisher != null)
            extinguisher.ResetForNextFire(classFFire, false);

        UpdateFireNameUI(GetDisplayFireName(breachFireIndex, classFFire));

        Debug.Log("[Challenge] Class F fire revealed after breach.");

        revealFireRoutine = null;
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

    int GetFireIndex(GameObject fireObject)
    {
        if (fireObject == null || fires == null)
            return -1;

        for (int i = 0; i < fires.Length; i++)
        {
            if (fires[i] == fireObject)
                return i;
        }

        return -1;
    }

    IEnumerator WinSequence()
    {
        yield return new WaitForSeconds(1.0f);

        if (uiManager != null)
            uiManager.ShowWin();
    }

    public GameObject GetCurrentFire()
    {
        if (CurrentFireIndex < 0 || fires == null || CurrentFireIndex >= fires.Length)
            return null;

        return fires[CurrentFireIndex];
    }

    public void RefreshCurrentExtinguisher()
    {
        var extinguisher = GetCurrentExtinguisherScript();
        var currentFire = GetCurrentFire();

        if (extinguisher != null)
            extinguisher.ResetForNextFire(currentFire, false);
    }

    ExtinguisherExtinguish_CameraRay GetCurrentExtinguisherScript()
    {
        if (extinguisherSwitcher == null)
            return null;

        return extinguisherSwitcher.GetCurrentExtinguisherScript();
    }

    void HideAllFires()
    {
        if (fires == null) return;

        foreach (var f in fires)
            HideFire(f);
    }

    void HideFire(GameObject fireRoot)
    {
        if (fireRoot == null) return;
        fireRoot.SetActive(false);
    }

    void ShowBreachDoor()
    {
        if (breachDoorObject == null) return;

        breachDoorObject.SetActive(true);

        foreach (var r in breachDoorObject.GetComponentsInChildren<Renderer>(true))
            r.enabled = true;

        Debug.Log("[Challenge] Breach door shown.");
    }

    void HideBreachDoor()
    {
        if (breachDoorObject == null) return;

        // Disable all renderers too, in case animator/state keeps visuals around
        foreach (var r in breachDoorObject.GetComponentsInChildren<Renderer>(true))
            r.enabled = false;

        breachDoorObject.SetActive(false);

        Debug.Log("[Challenge] Breach door hidden.");
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

        Debug.Log("[Challenge] Fire burst shown.");
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

        Debug.Log("[Challenge] Fire burst hidden.");
    }

    void UpdateTimerUI()
    {
        if (timerBar == null) return;

        int secs = Mathf.CeilToInt(TimeLeft);
        int mm = secs / 60;
        int ss = secs % 60;

        timerBar.highValue = timeLimitSeconds;
        timerBar.value = TimeLeft;
        timerBar.title = $"Timer: {mm:00}:{ss:00}";
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

    string GetDisplayFireName(int index, GameObject fire)
    {
        return fire != null ? fire.name : $"Fire {index}";
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