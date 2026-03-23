using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;

/**
 * FireChallenge
 * A simple manager for a fire-extinguishing challenge in Unity.
 * Rules:
 * - Player must extinguish a certain number of fires within a time limit.
 * - Fires are shown one at a time. When one is extinguished, the next appears.
 * - If time runs out, player loses.
 * - After all fires are extinguished, the door appears.
 * - Player manually breaches the door to complete the challenge and win.
 */

public class FireChallengeManager : MonoBehaviour
{
    [Header("Challenge Rules")]
    public int firesToWin = 5;
    public float timeLimitSeconds = 45f;

    [Header("Fires (drag 5+ fire roots here)")]
    public GameObject[] fires;

    [Header("Door")]
    public GameObject doorRoot;
    public DoorBreachController doorController;

    [Header("References")]
    public ExtinguisherModelSwitcher extinguisherSwitcher;
    public GameUIManager uiManager;
    public UIDocument uiDocument;

    [Header("UI Toolkit")]
    public string timerBarName = "timerBar";
    public string fireExtinguishedLabelName = "FireExtinguished";

    [Header("On-screen HUD (debug style)")]
    public bool showHUD = true;
    public int hudFontSize = 20;

    private int extinguishedCount = 0;
    private float timeLeft;
    private bool running = false;
    private bool waitingForDoorBreach = false;

    private ProgressBar timerBar;
    private Label fireExtinguishedLabel;

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

            if (timerBar != null)
            {
                timerBar.lowValue = 0f;
                timerBar.highValue = timeLimitSeconds;
            }
            else
            {
                Debug.LogWarning($"[Challenge] ProgressBar named '{timerBarName}' not found.");
            }

            if (fireExtinguishedLabel == null)
            {
                Debug.LogWarning($"[Challenge] Label named '{fireExtinguishedLabelName}' not found.");
            }
        }

        HideAllFires();
        HideDoor();
        ResetChallenge();
        UpdateTimerUI();
        UpdateFireExtinguishedUI();
    }

    public void StartChallenge()
    {
        ResetChallenge();
        running = true;
        ActivateFire(0);
        UpdateTimerUI();
        UpdateFireExtinguishedUI();
    }

    void Update()
    {
        if (!running) return;

        timeLeft -= Time.deltaTime;

        if (timeLeft <= 0f)
        {
            timeLeft = 0f;
            running = false;
            waitingForDoorBreach = false;

            HideAllFires();
            HideDoor();
            UpdateTimerUI();

            if (uiManager) uiManager.ShowLose();
            return;
        }

        UpdateTimerUI();
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
            waitingForDoorBreach = true;

            HideAllFires();
            UpdateTimerUI();
            ShowDoor();

            Debug.Log("[Challenge] All fires extinguished. Waiting for manual door breach.");
            if (doorController.hasBreached)
            {
                Debug.LogWarning("[Challenge] Door was already breached before waitingForDoorBreach was set. This may cause issues.");
            }
            return;
        }

        ActivateFire(extinguishedCount);
    }

    public void BreachDoorAndWin()
    {
        if (!waitingForDoorBreach)
        {
            Debug.LogWarning("[Challenge] Cannot breach yet. Not in door breach stage.");
            return;
        }

        if (doorController == null)
        {
            Debug.LogWarning("[Challenge] No doorController assigned.");
            return;
        }

        doorController.BreachDoor();
        waitingForDoorBreach = false;

        StartCoroutine(BreachDoorSequence());
    }

    IEnumerator BreachDoorSequence()
    {
        doorController.BreachDoor();
        waitingForDoorBreach = false;

        // wait for animation
        yield return new WaitForSeconds(1.2f);

        if (uiManager)
            uiManager.ShowWin();

        Debug.Log("[Challenge] Door breached. Win panel shown.");
    }

    public GameObject GetCurrentFire()
    {
        if (fires == null || fires.Length == 0) return null;
        if (extinguishedCount < 0 || extinguishedCount >= fires.Length) return null;
        return fires[extinguishedCount];
    }

    public void RefreshCurrentExtinguisher()
    {
        var extinguisher = extinguisherSwitcher ? extinguisherSwitcher.GetCurrentExtinguisher() : null;
        var currentFire = GetCurrentFire();

        if (extinguisher != null && currentFire != null)
        {
            extinguisher.ResetForNextFire(currentFire, false);
            Debug.Log($"[Challenge] Refreshed active extinguisher with current fire: {currentFire.name}");
        }
        else
        {
            Debug.LogWarning("[Challenge] Could not refresh extinguisher. Active extinguisher or current fire is null.");
        }
    }

    void ResetChallenge()
    {
        extinguishedCount = 0;
        timeLeft = timeLimitSeconds;
        running = false;
        waitingForDoorBreach = false;

        HideAllFires();
        HideDoor();

        if (doorController)
            doorController.ResetBreach();

        var extinguisher = extinguisherSwitcher ? extinguisherSwitcher.GetCurrentExtinguisher() : null;
        if (extinguisher)
            extinguisher.ResetForNextFire(null, true);

        UpdateTimerUI();
        UpdateFireExtinguishedUI();
    }

    void ActivateFire(int index)
    {
        if (fires == null || fires.Length == 0) return;

        if (index < 0) index = 0;
        if (index >= fires.Length) index = fires.Length - 1;

        HideAllFires();

        GameObject fire = fires[index];
        if (!fire) return;

        ShowFire(fire);

        var extinguisher = extinguisherSwitcher ? extinguisherSwitcher.GetCurrentExtinguisher() : null;
        if (extinguisher)
            extinguisher.ResetForNextFire(fire, true);
        else
            Debug.LogWarning("[Challenge] No active extinguisher found from switcher.");

        Debug.Log($"[Challenge] Activated fire index {index}: {fire.name}");
    }

    void HideAllFires()
    {
        if (fires == null) return;

        foreach (var f in fires)
            HideFire(f);
    }

    void HideFire(GameObject fireRoot)
    {
        if (!fireRoot) return;

        foreach (var r in fireRoot.GetComponentsInChildren<Renderer>(true))
            r.enabled = false;
    }

    void ShowFire(GameObject fireRoot)
    {
        if (!fireRoot) return;

        foreach (var r in fireRoot.GetComponentsInChildren<Renderer>(true))
            r.enabled = true;
    }

    void HideDoor()
    {
        if (!doorRoot) return;

        doorRoot.SetActive(false);
        Debug.Log("[Challenge] Door hidden");
    }

    void ShowDoor()
    {
        if (!doorRoot) return;

        doorRoot.SetActive(true);
        Debug.Log("[Challenge] Door shown");
    }

    public void ResetAndStart()
    {
        if (uiManager && uiManager.losePanel) uiManager.losePanel.SetActive(false);
        if (uiManager && uiManager.winPanel) uiManager.winPanel.SetActive(false);

        StartChallenge();
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
        if (fireExtinguishedLabel == null) return;

        fireExtinguishedLabel.text = $"Fires: {extinguishedCount}/{firesToWin}";
    }
}