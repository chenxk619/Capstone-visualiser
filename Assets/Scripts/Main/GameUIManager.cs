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
    public string cheatButtonName = "cheatButton";

    private Button cheatButton;

    void Start()
    {
        if (winPanel) winPanel.SetActive(false);
        if (losePanel) losePanel.SetActive(false);
        if (startPanel) startPanel.SetActive(true);

        SetupCheatButton();
    }

    void SetupCheatButton()
    {
        if (uiDocument == null)
        {
            Debug.LogWarning("[GameUIManager] No UIDocument assigned for cheat button.");
            return;
        }

        var root = uiDocument.rootVisualElement;
        cheatButton = root.Q<Button>(cheatButtonName);

        if (cheatButton == null)
        {
            Debug.LogWarning($"[GameUIManager] Button '{cheatButtonName}' not found.");
            return;
        }

        cheatButton.clicked -= StartCheatGame;
        cheatButton.clicked += StartCheatGame;
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

    public void Restart()
    {
        Debug.Log("[GameUIManager] RESTART CLICKED");

        if (winPanel) winPanel.SetActive(false);
        if (losePanel) losePanel.SetActive(false);
        if (startPanel) startPanel.SetActive(false);

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

    public void StartCheatGame()
    {
        Debug.Log("[GameUIManager] START CHEAT MODE CLICKED");

        if (winPanel) winPanel.SetActive(false);
        if (losePanel) losePanel.SetActive(false);
        if (startPanel) startPanel.SetActive(false);

        if (challengeManager != null)
        {
            challengeManager.StartCheatMode();
            return;
        }

        Debug.LogWarning("[GameUIManager] No FireChallengeManager assigned.");
    }

    public void StartTutorial()
    {
        Debug.Log("[GameUIManager] START TUTORIAL CLICKED");

        if (winPanel) winPanel.SetActive(false);
        if (losePanel) losePanel.SetActive(false);
        if (startPanel) startPanel.SetActive(false);

        if (tutorialManager != null)
        {
            tutorialManager.OpenTutorial();
        }
        else
        {
            Debug.LogWarning("[GameUIManager] No UITutorialManager assigned.");
        }
    }
}