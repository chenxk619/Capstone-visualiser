using UnityEngine;
using UnityEngine.UIElements;

public class DoorBreachUIController : MonoBehaviour
{
    [Header("UI")]
    public UIDocument uiDocument;
    public string breachButtonName = "BreachButton";

    [Header("Challenge")]
    public FireChallengeManager challengeManager;

    [Header("Fallback Door")]
    public DoorBreachController doorBreachController;

    private Button breachButton;

    void Start()
    {
        if (uiDocument == null)
        {
            Debug.LogWarning("[DoorBreachUIController] UIDocument is not assigned.");
            return;
        }

        var root = uiDocument.rootVisualElement;
        breachButton = root.Q<Button>(breachButtonName);

        if (breachButton == null)
        {
            Debug.LogWarning($"[DoorBreachUIController] Button named '{breachButtonName}' not found.");
            return;
        }

        breachButton.clicked += OnBreachClicked;

        Debug.Log("[DoorBreachUIController] Breach button linked successfully.");
    }

    void OnBreachClicked()
    {
        TriggerBreach();
    }

    public void TriggerBreach()
    {
        Debug.Log("[DoorBreachUIController] Breach triggered.");

        if (challengeManager != null)
        {
            challengeManager.ExecuteBreach();
            return;
        }

        if (doorBreachController != null)
        {
            doorBreachController.BreachDoor();
            return;
        }

        Debug.LogWarning("[DoorBreachUIController] No FireChallengeManager or DoorBreachController assigned.");
    }
}