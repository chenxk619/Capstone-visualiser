using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;

/**
 * FireChallenge
 * Rules:
 * - Player must extinguish a certain number of fires within a time limit.
 * - Fires are shown one at a time. When one is extinguished, the next appears.
 * - If time runs out, player loses.
 * - After all fires are extinguished, the final action stage begins.
 * - Player must execute BOTH Breach and Block to win.
 */

public class FireChallengeManager : MonoBehaviour
{
    [Header("Challenge Rules")]
    public int firesToWin = 5;
    public float timeLimitSeconds = 6000f;

    [Header("Fires (drag 5+ fire roots here)")]
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

    [Header("On-screen HUD (debug style)")]
    public bool showHUD = true;
    public int hudFontSize = 20;

    private int extinguishedCount = 0;
    private float timeLeft;
    private bool running = false;

    // Final stage flags
    private bool waitingForFinalActions = false;
    private bool BreachDone = false;
    private bool blockDone = false;
    private bool winTriggered = false;

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
        if (doorRoot != null)
        {
            doorRoot.SetActive(false);
        }

        if (doorController != null)
        {
            doorController.hasBreached = false;
        }
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
            waitingForFinalActions = false;

            HideAllFires();
            HideDoor();
            UpdateTimerUI();

            if (uiManager) uiManager.ShowLose();
            Debug.Log("[Challenge] Time up. Lose.");
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
            // Enter final action stage
            HideAllFires();
            ShowDoor();

            waitingForFinalActions = true;
            BreachDone = false;
            blockDone = false;

            Debug.Log("[Challenge] All fires extinguished. Waiting for BOTH Breach and Block.");
            return;
        }

        ActivateFire(extinguishedCount);
    }

    /// <summary>
    /// Call this when the player performs Breach.
    /// Hook this to your Breach button / synced command / animation event.
    /// </summary>
    public void ExecuteBreach()
    {
        if (!waitingForFinalActions)
        {
            Debug.LogWarning("[Challenge] Breach ignored. Not in final action stage.");
            return;
        }

        if (BreachDone)
        {
            Debug.Log("[Challenge] Breach already completed.");
            return;
        }

        BreachDone = true;
        Debug.Log("[Challenge] Breach completed.");

        // Optional: trigger door animation here
        if (doorController != null && !doorController.hasBreached)
        {
            doorController.BreachDoor();
        }

        CheckFinalWinCondition();
    }

    /// <summary>
    /// Call this when the player performs BLOCK.
    /// Hook this to your block button / synced command / animation event.
    /// </summary>
    public void ExecuteBlock()
    {
        if (!waitingForFinalActions)
        {
            Debug.LogWarning("[Challenge] Block ignored. Not in final action stage.");
            return;
        }

        if (blockDone)
        {
            Debug.Log("[Challenge] Block already completed.");
            return;
        }

        blockDone = true;
        Debug.Log("[Challenge] Block completed.");

        riotShieldController.TriggerBlockShield();

        CheckFinalWinCondition();
    }

    void CheckFinalWinCondition()
    {
        Debug.Log($"[Challenge] Final action status => Breach: {BreachDone}, Block: {blockDone}");

        if (!waitingForFinalActions || winTriggered)
            return;

        if (BreachDone && blockDone)
        {
            winTriggered = true;
            waitingForFinalActions = false;
            running = false;

            StartCoroutine(WinSequence());
        }
    }

    IEnumerator WinSequence()
    {
        // Wait a bit if you want the Breach/breach animation to finish
        yield return new WaitForSeconds(1.2f);

        if (uiManager)
            uiManager.ShowWin();

        Debug.Log("[Challenge] Both Breach and Block completed. Win panel shown.");
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

        waitingForFinalActions = false;
        BreachDone = false;
        blockDone = false;
        winTriggered = false;

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