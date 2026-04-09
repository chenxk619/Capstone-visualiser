using UnityEngine;
using UnityEngine.UIElements;

public class GameUIManager : MonoBehaviour
{
    [Header("UI")]
    public GameObject winPanel;
    public GameObject startPanel;
    public GameObject losePanel;

    [Header("Gameplay")]
    public ExtinguisherModelSwitcher extinguisherSwitcher;
    public FireChallengeManager challengeManager;
    public UITutorialManager tutorialManager;

    [Header("UI Toolkit")]
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

        // Start panel / menu state
        SetManualGameplayButtonsVisible(true);
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
    }

    void SetupCheatButton()
    {
        if (cheatButton == null)
        {
            Debug.LogWarning($"[GameUIManager] Button '{cheatButtonName}' not found.");
            return;
        }

        cheatButton.clicked -= StartCheatGame;
        cheatButton.clicked += StartCheatGame;
    }

    void SetButtonVisible(Button btn, bool visible)
    {
        if (btn == null) return;
        btn.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
    }

    public void SetManualGameplayButtonsVisible(bool visible)
    {
        SetButtonVisible(blockButton, visible);
        SetButtonVisible(breachButton, visible);
        SetButtonVisible(pinButton, visible);
        SetButtonVisible(togglePressureButton, visible);
        SetButtonVisible(prevExtinguisherButton, visible);
        SetButtonVisible(nextExtinguisherButton, visible);

        Debug.Log($"[GameUIManager] Manual gameplay buttons visible = {visible}");
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

        // Hide manual buttons in real gameplay mode
        SetManualGameplayButtonsVisible(false);

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

    // Cheat mode = manual UI buttons allowed
    public void StartCheatGame()
    {
        Debug.Log("[GameUIManager] START CHEAT MODE CLICKED");

        if (winPanel) winPanel.SetActive(false);
        if (losePanel) losePanel.SetActive(false);
        if (startPanel) startPanel.SetActive(false);

        // Show manual buttons in cheat mode
        SetManualGameplayButtonsVisible(true);

        if (challengeManager != null)
        {
            challengeManager.StartCheatMode();
            return;
        }

        Debug.LogWarning("[GameUIManager] No FireChallengeManager assigned.");
    }

    // Tutorial = manual UI buttons allowed
    public void StartTutorial()
    {
        Debug.Log("[GameUIManager] START TUTORIAL CLICKED");

        if (winPanel) winPanel.SetActive(false);
        if (losePanel) losePanel.SetActive(false);
        if (startPanel) startPanel.SetActive(false);

        SetManualGameplayButtonsVisible(true);

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

        // Menu state: visible
        SetManualGameplayButtonsVisible(true);
    }
}