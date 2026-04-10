using UnityEngine;
using UnityEngine.UIElements;

public class PressureToggleUIController : MonoBehaviour
{
    [Header("UI")]
    public UIDocument uiDocument;
    public string togglePressureButtonName = "togglePressureButton";

    [Header("Extinguisher")]
    public ExtinguisherExtinguish_CameraRay extinguisher;

    private Button togglePressureButton;

    void Start()
    {
        if (uiDocument == null)
        {
            Debug.LogWarning("[PressureToggleUIController] UIDocument is not assigned.");
            return;
        }

        var root = uiDocument.rootVisualElement;
        togglePressureButton = root.Q<Button>(togglePressureButtonName);

        if (togglePressureButton == null)
        {
            Debug.LogWarning($"[PressureToggleUIController] Button named '{togglePressureButtonName}' not found.");
            return;
        }

        togglePressureButton.clicked += TogglePressureMode;

        Debug.Log("[PressureToggleUIController] Pressure toggle button linked successfully.");
    }

    void TogglePressureMode()
    {
        if (extinguisher == null)
        {
            Debug.LogWarning("[PressureToggleUIController] No extinguisher assigned.");
            return;
        }

        bool newMode = !extinguisher.IsPressureMode();
        extinguisher.SetPressureMode(newMode);

        Debug.Log($"[PressureToggleUIController] Pressure mode toggled to {newMode}");
    }

}