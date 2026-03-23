using UnityEngine;
using UnityEngine.UIElements;

public class RiotShieldUIController : MonoBehaviour
{
    [Header("UI")]
    public UIDocument uiDocument;
    public string blockButtonName = "BlockButton";

    [Header("Shield")]
    public RiotShieldController riotShieldController;

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
        if (riotShieldController == null)
        {
            Debug.LogWarning("[RiotShieldUIController] RiotShieldController not assigned.");
            return;
        }

        Debug.Log("[RiotShieldUIController] Block triggered.");
        riotShieldController.TriggerBlockShield();
    }
}