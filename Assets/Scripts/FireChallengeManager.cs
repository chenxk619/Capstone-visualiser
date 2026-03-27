using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;
using System.Collections.Generic;

public class FireChallengeManager : MonoBehaviour
{
    [Header("Challenge Rules")]
    public int firesToWin = 5;
    public float timeLimitSeconds = 600f;

    [Header("Fires (match extinguisher index order)")]
    public GameObject[] fires;

    [Header("Optional Fire Type Switcher")]
    public FireTypeSwitcher fireTypeSwitcher;

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

    [Header("Sequence")]
    public bool randomizeFireOrder = false;

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

    private List<int> fireSequence = new List<int>();

    public int CurrentFireIndex { get; private set; } = -1;
    public int ExtinguishedCount => extinguishedCount;
    public float TimeLeft => Mathf.Max(0f, timeLeft);
    public bool Running => running;

    void Start()
    {
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
        BuildFireSequence();

        running = true;
        ActivateFire(0);

        UpdateTimerUI();
        UpdateFireExtinguishedUI();
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

        HideAllFires();
        HideDoor();

        if (doorController != null)
            doorController.ResetBreach();

        var extinguisher = GetCurrentExtinguisherScript();
        if (extinguisher != null)
            extinguisher.ResetForNextFire(null, true);

        UpdateTimerUI();
        UpdateFireExtinguishedUI();
        UpdateFireNameUI("");
    }

    void BuildFireSequence()
    {
        fireSequence.Clear();

        if (fires == null || fires.Length == 0)
            return;

        for (int i = 0; i < fires.Length; i++)
            fireSequence.Add(i);

        if (randomizeFireOrder)
        {
            for (int i = 0; i < fireSequence.Count; i++)
            {
                int j = Random.Range(i, fireSequence.Count);
                int temp = fireSequence[i];
                fireSequence[i] = fireSequence[j];
                fireSequence[j] = temp;
            }
        }

        Debug.Log("[Challenge] Fire sequence: " + string.Join(", ", fireSequence));
    }

    void ActivateFire(int sequenceIndex)
    {
        if (fires == null || fires.Length == 0) return;
        if (fireSequence == null || fireSequence.Count == 0) BuildFireSequence();
        if (fireSequence.Count == 0) return;

        if (sequenceIndex < 0) sequenceIndex = 0;
        if (sequenceIndex >= fireSequence.Count) sequenceIndex = fireSequence.Count - 1;

        int fireArrayIndex = fireSequence[sequenceIndex];
        if (fireArrayIndex < 0 || fireArrayIndex >= fires.Length) return;

        CurrentFireIndex = fireArrayIndex;

        HideAllFires();

        GameObject fire = fires[fireArrayIndex];
        if (fire == null)
        {
            Debug.LogWarning($"[Challenge] Fire at index {fireArrayIndex} is null.");
            return;
        }

        ShowOnlyCurrentFire(fireArrayIndex, fire);
        UpdateFireNameUI(GetDisplayFireName(fireArrayIndex, fire));

        var extinguisher = GetCurrentExtinguisherScript();
        if (extinguisher != null)
        {
            extinguisher.ResetForNextFire(fire, true);
        }
        else
        {
            Debug.LogWarning("[Challenge] No active extinguisher script found.");
        }

        Debug.Log($"[Challenge] Activated fire sequence {sequenceIndex} -> fire index {fireArrayIndex}: {fire.name}");
    }

    void ShowOnlyCurrentFire(int fireIndex, GameObject fire)
    {
        if (fireTypeSwitcher != null)
        {
            fireTypeSwitcher.ShowFireByIndex(fireIndex);
            return;
        }

        ShowFire(fire);
    }

    public void OnFireExtinguished()
    {
        if (!running) return;

        extinguishedCount++;
        UpdateFireExtinguishedUI();

        Debug.Log($"[Challenge] Fire extinguished count = {extinguishedCount}");

        if (extinguishedCount >= firesToWin)
        {
            running = false;
            waitingForFinalActions = true;
            breachDone = false;
            blockDone = false;

            HideAllFires();
            ShowDoor();
            UpdateFireNameUI("Door");

            Debug.Log("[Challenge] All fires extinguished. Waiting for BOTH Breach and Block.");
            return;
        }

        ActivateFire(extinguishedCount);
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
        if (fireTypeSwitcher != null)
            fireTypeSwitcher.HideAll();

        if (fires == null) return;

        foreach (var f in fires)
            HideFire(f);
    }

    void HideFire(GameObject fireRoot)
    {
        if (fireRoot == null) return;
        fireRoot.SetActive(false);
    }

    void ShowFire(GameObject fireRoot)
    {
        if (fireRoot == null) return;
        fireRoot.SetActive(true);
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
        return fire != null ? fire.name : "";
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