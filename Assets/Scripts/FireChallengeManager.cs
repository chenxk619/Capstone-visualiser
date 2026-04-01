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

    [Header("Door")]
    public GameObject doorRoot;
    public DoorBreachController doorController;

    [Header("Shield")]
    public GameObject shieldRoot;
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

    private bool waitingForFinalActions = false;
    private bool breachDone = false;
    private bool blockDone = false;
    private bool winTriggered = false;

    // Tracks whether each fire index has already been extinguished
    private bool[] extinguishedFlags;

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

        timeLeft -= Time.deltaTime;
        if (timeLeft <= 0f)
        {
            timeLeft = 0f;
            running = false;
            waitingForFinalActions = false;

            HideAllFires();
            HideDoor();
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

        waitingForFinalActions = false;
        breachDone = false;
        blockDone = false;
        winTriggered = false;

        CurrentFireIndex = -1;

        if (extinguishedFlags == null || extinguishedFlags.Length != ((fires != null && fires.Length > 0) ? fires.Length : 5))
            extinguishedFlags = new bool[(fires != null && fires.Length > 0) ? fires.Length : 5];

        for (int i = 0; i < extinguishedFlags.Length; i++)
            extinguishedFlags[i] = false;

        HideAllFires();
        HideDoor();

        if (doorController != null)
            doorController.ResetBreach();

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
        if (waitingForFinalActions) return;
        if (fireIndex < 0) return;
        if (IsFireAlreadyExtinguished(fireIndex))
        {
            if (fire != null)
                fire.SetActive(false);
            return;
        }

        CurrentFireIndex = fireIndex;

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

        var extinguisher = GetCurrentExtinguisherScript();
        if (extinguisher != null)
            extinguisher.ResetForNextFire(null, false);

        UpdateFireNameUI("");

        Debug.Log($"[Challenge] Lost tracking for fire index {fireIndex}");
    }

    public void OnFireExtinguished(GameObject extinguishedFire)
    {
        if (!running) return;
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

        if (extinguishedCount >= firesToWin)
        {
            running = false;
            waitingForFinalActions = true;
            breachDone = false;
            blockDone = false;

            HideAllFires();
            ShowDoor();
            UpdateFireNameUI("Door");

            Debug.Log("[Challenge] All required fires extinguished. Waiting for BOTH Breach and Block.");
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

    public void ExecuteBreach()
    {
        if (!waitingForFinalActions) return;
        if (breachDone) return;

        breachDone = true;

        if (doorController != null && !doorController.hasBreached)
            doorController.BreachDoor();

        CheckFinalWinCondition();
    }

    public void ExecuteBlock()
    {
        if (!waitingForFinalActions) return;
        if (blockDone) return;

        blockDone = true;

        if (riotShieldController != null)
            riotShieldController.TriggerBlockShield();

        CheckFinalWinCondition();
    }

    void CheckFinalWinCondition()
    {
        if (!waitingForFinalActions || winTriggered) return;

        if (breachDone && blockDone)
        {
            winTriggered = true;
            waitingForFinalActions = false;
            StartCoroutine(WinSequence());
        }
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

    void HideDoor()
    {
        if (doorRoot != null)
            doorRoot.SetActive(false);
    }

    void ShowDoor()
    {
        if (doorRoot != null)
            doorRoot.SetActive(true);
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
}