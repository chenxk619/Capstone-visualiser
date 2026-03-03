using UnityEngine;

public class GameUIManager : MonoBehaviour
{
    [Header("UI")]
    public GameObject winPanel;

    [Header("Fire")]
    public GameObject fireRoot;

    [Header("Gameplay")]
    public ExtinguisherExtinguish_CameraRay extinguisherScript;

    void Start()
    {
        if (winPanel) winPanel.SetActive(false);
    }

    public void ShowWin()
    {
        if (winPanel) winPanel.SetActive(true);
    }

    public bool IsWinShowing()
    {
        return winPanel != null && winPanel.activeInHierarchy;
    }

    public void Restart()
    {
        Debug.Log("RESTART CLICKED");

        if (winPanel) winPanel.SetActive(false);

        if (fireRoot)
        {
            fireRoot.SetActive(true);
            foreach (var r in fireRoot.GetComponentsInChildren<Renderer>(true))
                r.enabled = true;
        }

        // ✅ IMPORTANT: Assign extinguisherScript in Inspector (no FindObjectOfType needed)
        if (extinguisherScript)
            extinguisherScript.ResetGame();
        else
            Debug.LogWarning("[GameUIManager] extinguisherScript is not assigned in Inspector.");
    }
}