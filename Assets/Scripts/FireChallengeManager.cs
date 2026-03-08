using UnityEngine;
using UnityEngine.UIElements;

/**
 * FireChallenge
 * A simple manager for a fire-extinguishing challenge in Unity.
 * Rules:
 * - Player must extinguish a certain number of fires within a time limit.
 * - Fires are shown one at a time. When one is extinguished, the next appears.
 * - If time runs out, player loses. If all fires are extinguished, player wins.
 */

public class FireChallengeManager : MonoBehaviour
{
    [Header("Challenge Rules")]
    public int firesToWin = 5;
    public float timeLimitSeconds = 30f;

    [Header("Fires (drag 5+ fire roots here)")]
    public GameObject[] fires;

    [Header("References")]
    public ExtinguisherExtinguish_CameraRay extinguisher;
    public GameUIManager uiManager;
    public UIDocument uiDocument;

    [Header("UI Toolkit")]
    public string timerBarName = "timerBar";

    [Header("On-screen HUD (debug style)")]
    public bool showHUD = true;
    public int hudFontSize = 20;

    private int extinguishedCount = 0;
    private float timeLeft;
    private bool running = false;

    private ProgressBar timerBar;

    public int ExtinguishedCount => extinguishedCount;
    public float TimeLeft => Mathf.Max(0f, timeLeft);
    public bool Running => running;

    void Start()
    {
        if (uiDocument != null)
        {
            var root = uiDocument.rootVisualElement;
            timerBar = root.Q<ProgressBar>(timerBarName);

            if (timerBar != null)
            {
                timerBar.lowValue = 0f;
                timerBar.highValue = timeLimitSeconds;
            }
        }

        HideAllFires();
        ResetChallenge();
        UpdateTimerUI();
    }

    public void StartChallenge()
    {
        ResetChallenge();
        running = true;
        ActivateFire(0);
        UpdateTimerUI();
    }

    void Update()
    {
        if (!running) return;

        timeLeft -= Time.deltaTime;

        if (timeLeft <= 0f)
        {
            timeLeft = 0f;
            running = false;

            HideAllFires();
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

        if (extinguishedCount >= firesToWin)
        {
            running = false;
            HideAllFires();
            UpdateTimerUI();

            if (uiManager) uiManager.ShowWin();
            return;
        }

        ActivateFire(extinguishedCount);
    }

    void ResetChallenge()
    {
        extinguishedCount = 0;
        timeLeft = timeLimitSeconds;
        running = false;

        HideAllFires();

        if (extinguisher)
            extinguisher.ResetForNextFire(null, true);

        UpdateTimerUI();
    }

    void ActivateFire(int index)
    {
        if (fires == null || fires.Length == 0) return;

        // safety clamp
        if (index < 0) index = 0;
        if (index >= fires.Length) index = fires.Length - 1;

        HideAllFires();

        GameObject fire = fires[index];
        if (!fire) return;

        ShowFire(fire);

        if (extinguisher)
            extinguisher.ResetForNextFire(fire, true);

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
}