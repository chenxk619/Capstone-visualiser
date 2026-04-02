using UnityEngine;
using UnityEngine.UIElements;

public class RiotShieldUIController : MonoBehaviour
{
    [Header("UI")]
    public UIDocument uiDocument;
    public string blockButtonName = "BlockButton";

    [Header("Shield")]
    public RiotShieldController riotShieldController;

    [Header("Challenge")]
    public FireChallengeManager challengeManager;

    private Button blockButton;

    void Start()
    {
        if (uiDocument == null)
        {
            Debug.LogWarning("[RiotShieldUIController] UIDocument is not assigned.");
            return;
        }

        var root = uiDocument.rootVisualElement;
        blockButton = root.Q<Button>(blockButtonName);

        if (blockButton == null)
        {
            Debug.LogWarning($"[RiotShieldUIController] Button named '{blockButtonName}' not found.");
            return;
        }

        blockButton.clicked += OnBlockClicked;

        Debug.Log("[RiotShieldUIController] Block button linked successfully.");
    }

    void OnBlockClicked()
    {
        TriggerBlock();
    }

    public void TriggerBlock()
    {
        Debug.Log("[RiotShieldUIController] Block triggered.");

        if (challengeManager != null)
        {
            challengeManager.ExecuteBlock();
            return;
        }

        // Fallback only if no challenge manager assigned
        if (riotShieldController != null)
            riotShieldController.TriggerBlockShield();
        else
            Debug.LogWarning("[RiotShieldUIController] No RiotShieldController or FireChallengeManager assigned.");
    }
}