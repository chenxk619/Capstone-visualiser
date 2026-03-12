/**

 * GameUIManager.cs
 * 
 * Manages the UI panels for win/lose/start states.
 * Also handles the "Try Again" button to restart the game or challenge.
 *

**/

using UnityEngine;

public class GameUIManager : MonoBehaviour
{
    [Header("UI")]
    public GameObject winPanel;
    public GameObject startPanel;
    public GameObject losePanel;

    [Header("Gameplay")]
    public ExtinguisherModelSwitcher extinguisherSwitcher;

    public FireChallengeManager challengeManager;

    void Start()
    {
        if (winPanel) winPanel.SetActive(false);
        if (losePanel) losePanel.SetActive(false);   // NEW
        if (startPanel) startPanel.SetActive(true);
    }

    public void ShowWin()
    {
        if (losePanel) losePanel.SetActive(false);
        if (winPanel) winPanel.SetActive(true);
    }

    public void ShowLose()
    {
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

    // This is the button callback for "Try Again" / "Restart"
    public void Restart()
    {
        Debug.Log("RESTART CLICKED");

        if (winPanel) winPanel.SetActive(false);
        if (losePanel) losePanel.SetActive(false);   // NEW
        if (startPanel) startPanel.SetActive(false);

        // If you are using the 5-in-30s challenge, restart the challenge
        if (challengeManager)
        {
            challengeManager.StartChallenge();
            return;
        }

        var extinguisher = extinguisherSwitcher ? extinguisherSwitcher.GetCurrentExtinguisher() : null;
        if (extinguisher)
            extinguisher.ResetGame();
        else
            Debug.LogWarning("[GameUIManager] No active extinguisher found from switcher.");
    }
}