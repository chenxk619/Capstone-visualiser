using UnityEngine;
using UnityEngine.UIElements;

public class GameUIManager : MonoBehaviour
{
    [Header("Panels")]
    public GameObject winPanel;
    public GameObject startPanel;
    public GameObject losePanel;

    [Header("Gameplay References")]
    public ExtinguisherModelSwitcher extinguisherSwitcher;
    public FireChallengeManager challengeManager;
    public UITutorialManager tutorialManager;

    [Header("UI Document")]
    public UIDocument uiDocument;

    [Header("Button Names")]
    public string cheatButtonName = "cheatButton";
    public string blockButtonName = "BlockButton";
    public string breachButtonName = "BreachButton";
    public string pinButtonName = "PinButton";
    public string togglePressureButtonName = "togglePressureButton";
    public string prevExtinguisherButtonName = "PrevExtinguisherButton";
    public string nextExtinguisherButtonName = "NextExtinguisherButton";

    private Button cheatButton;
    private Button blockButton;
    private Button breachButton;
    private Button pinButton;
    private Button togglePressureButton;
    private Button prevExtinguisherButton;
    private Button nextExtinguisherButton;

    void Start()
    {
        if (winPanel) winPanel.SetActive(false);
        if (losePanel) losePanel.SetActive(false);
        if (startPanel) startPanel.SetActive(true);

        CacheButtons();
        SetupCheatButton();

        // Start menu state: manual buttons available
        SetButtonsForManualMode();
    }

    void CacheButtons()
    {
        if (uiDocument == null)
        {
            Debug.LogWarning("[GameUIManager] No UIDocument assigned.");
            return;
        }

        var root = uiDocument.rootVisualElement;
        if (root == null)
        {
            Debug.LogWarning("[GameUIManager] rootVisualElement is null.");
            return;
        }

        cheatButton = root.Q<Button>(cheatButtonName);
        blockButton = root.Q<Button>(blockButtonName);
        breachButton = root.Q<Button>(breachButtonName);
        pinButton = root.Q<Button>(pinButtonName);
        togglePressureButton = root.Q<Button>(togglePressureButtonName);
        prevExtinguisherButton = root.Q<Button>(prevExtinguisherButtonName);
        nextExtinguisherButton = root.Q<Button>(nextExtinguisherButtonName);

        if (cheatButton == null) Debug.LogWarning($"[GameUIManager] Button '{cheatButtonName}' not found.");
        if (blockButton == null) Debug.LogWarning($"[GameUIManager] Button '{blockButtonName}' not found.");
        if (breachButton == null) Debug.LogWarning($"[GameUIManager] Button '{breachButtonName}' not found.");
        if (pinButton == null) Debug.LogWarning($"[GameUIManager] Button '{pinButtonName}' not found.");
        if (togglePressureButton == null) Debug.LogWarning($"[GameUIManager] Button '{togglePressureButtonName}' not found.");
        if (prevExtinguisherButton == null) Debug.LogWarning($"[GameUIManager] Button '{prevExtinguisherButtonName}' not found.");
        if (nextExtinguisherButton == null) Debug.LogWarning($"[GameUIManager] Button '{nextExtinguisherButtonName}' not found.");
    }

    void SetupCheatButton()
    {
        if (cheatButton == null)
            return;

        cheatButton.clicked -= StartCheatGame;
        cheatButton.clicked += StartCheatGame;
    }

    void SetButtonVisible(Button btn, bool visible)
    {
        if (btn == null) return;
        btn.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
    }

    void SetButtonEnabled(Button btn, bool enabled)
    {
        if (btn == null) return;
        btn.SetEnabled(enabled);
        btn.style.opacity = enabled ? 1f : 0.5f;
    }

    public void SetButtonsForNormalGameplay()
    {
        // Hidden entirely in hardware/comms-only gameplay
        SetButtonVisible(blockButton, false);
        SetButtonVisible(breachButton, false);
        SetButtonVisible(prevExtinguisherButton, false);
        SetButtonVisible(nextExtinguisherButton, false);

        // Shown but disabled
        SetButtonVisible(pinButton, true);
        SetButtonEnabled(pinButton, false);

        SetButtonVisible(togglePressureButton, true);
        SetButtonEnabled(togglePressureButton, false);

        Debug.Log("[GameUIManager] Normal gameplay UI applied.");
    }

    public void SetButtonsForManualMode()
    {
        SetButtonVisible(blockButton, true);
        SetButtonEnabled(blockButton, true);

        SetButtonVisible(breachButton, true);
        SetButtonEnabled(breachButton, true);

        SetButtonVisible(pinButton, true);
        SetButtonEnabled(pinButton, true);

        SetButtonVisible(togglePressureButton, true);
        SetButtonEnabled(togglePressureButton, true);

        SetButtonVisible(prevExtinguisherButton, true);
        SetButtonEnabled(prevExtinguisherButton, true);

        SetButtonVisible(nextExtinguisherButton, true);
        SetButtonEnabled(nextExtinguisherButton, true);

        Debug.Log("[GameUIManager] Manual mode UI applied.");
    }

    public void ShowWin()
    {
        if (losePanel) losePanel.SetActive(false);
        if (winPanel) winPanel.SetActive(true);
    }

    public void ShowLose()
    {
        if (challengeManager != null && challengeManager.cheatMode)
        {
            Debug.Log("[GameUIManager] Cheat mode ON - lose screen blocked.");
            return;
        }

        if (winPanel) winPanel.SetActive(false);
        if (losePanel) losePanel.SetActive(true);
    }

    public bool IsWinShowing()
    {
        return winPanel != null && winPanel.activeInHierarchy;
    }

    public bool IsLoseShowing()
    {
        return losePanel != null && losePanel.activeInHierarchy;
    }

    // Normal gameplay = hardware/comms only
    public void Restart()
    {
        Debug.Log("[GameUIManager] RESTART CLICKED");

        if (winPanel) winPanel.SetActive(false);
        if (losePanel) losePanel.SetActive(false);
        if (startPanel) startPanel.SetActive(false);

        SetButtonsForNormalGameplay();

        if (challengeManager != null)
        {
            challengeManager.cheatMode = false;
            challengeManager.StartChallenge();
            return;
        }

        var extinguisher = extinguisherSwitcher != null
            ? extinguisherSwitcher.GetCurrentExtinguisherScript()
            : null;

        if (extinguisher != null)
        {
            extinguisher.ResetGame();
        }
        else
        {
            Debug.LogWarning("[GameUIManager] No active extinguisher script found.");
        }
    }

    // Cheat mode = manual controls allowed
    public void StartCheatGame()
    {
        Debug.Log("[GameUIManager] START CHEAT MODE CLICKED");

        if (winPanel) winPanel.SetActive(false);
        if (losePanel) losePanel.SetActive(false);
        if (startPanel) startPanel.SetActive(false);

        SetButtonsForManualMode();

        if (challengeManager != null)
        {
            challengeManager.StartCheatMode();
            return;
        }

        Debug.LogWarning("[GameUIManager] No FireChallengeManager assigned.");
    }

    // Tutorial = manual controls allowed
    public void StartTutorial()
    {
        Debug.Log("[GameUIManager] START TUTORIAL CLICKED");

        if (winPanel) winPanel.SetActive(false);
        if (losePanel) losePanel.SetActive(false);
        if (startPanel) startPanel.SetActive(false);

        SetButtonsForManualMode();

        if (tutorialManager != null)
        {
            tutorialManager.OpenTutorial();
        }
        else
        {
            Debug.LogWarning("[GameUIManager] No UITutorialManager assigned.");
        }
    }

    public void ReturnToStartPanel()
    {
        if (winPanel) winPanel.SetActive(false);
        if (losePanel) losePanel.SetActive(false);
        if (startPanel) startPanel.SetActive(true);

        SetButtonsForManualMode();
    }
}