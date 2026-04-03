using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class UITutorialManager : MonoBehaviour
{
    [Header("UI")]
    public UIDocument uiDocument;

    [Header("Tutorial Steps")]
    public List<UITutorialStep> steps = new List<UITutorialStep>();

    public GameObject startPanelCanvas;

    private VisualElement root;
    private VisualElement tutorialOverlay;
    private VisualElement tutorialHighlight;
    private Label tutorialArrow;
    private VisualElement tutorialTextPanel;
    private Label tutorialTitle;
    private Label tutorialBody;
    private Button tutorialNextButton;
    private Button tutorialCloseButton;

    private int currentStepIndex = 0;
    private bool tutorialOpen = false;

    void Start()
    {
        if (uiDocument == null)
        {
            Debug.LogWarning("[UITutorialManager] UIDocument is not assigned.");
            return;
        }

        root = uiDocument.rootVisualElement;

        tutorialOverlay = root.Q<VisualElement>("TutorialOverlay");
        tutorialHighlight = root.Q<VisualElement>("TutorialHighlight");
        tutorialArrow = root.Q<Label>("TutorialArrow");
        tutorialTextPanel = root.Q<VisualElement>("TutorialTextPanel");
        tutorialTitle = root.Q<Label>("TutorialTitle");
        tutorialBody = root.Q<Label>("TutorialBody");
        tutorialNextButton = root.Q<Button>("TutorialNextButton");
        tutorialCloseButton = root.Q<Button>("TutorialCloseButton");

        if (tutorialNextButton != null)
            tutorialNextButton.clicked += NextStep;

        if (tutorialCloseButton != null)
            tutorialCloseButton.clicked += CloseTutorial;

        HideTutorial();
    }

    public void OpenTutorial()
    {
        if (steps == null || steps.Count == 0)
        {
            Debug.LogWarning("[UITutorialManager] No tutorial steps assigned.");
            return;
        }

        tutorialOpen = true;
        currentStepIndex = 0;
        ShowCurrentStep();
    }

    public void CloseTutorial()
    {
        tutorialOpen = false;
        HideTutorial();

        if (startPanelCanvas != null)
            startPanelCanvas.SetActive(true);
    }

    void HideTutorial()
    {
        if (tutorialOverlay != null) tutorialOverlay.style.display = DisplayStyle.None;
        if (tutorialHighlight != null) tutorialHighlight.style.display = DisplayStyle.None;
        if (tutorialArrow != null) tutorialArrow.style.display = DisplayStyle.None;
        if (tutorialTextPanel != null) tutorialTextPanel.style.display = DisplayStyle.None;
    }

    void ShowCurrentStep()
    {
        if (!tutorialOpen || steps == null || steps.Count == 0)
            return;

        if (currentStepIndex < 0 || currentStepIndex >= steps.Count)
        {
            CloseTutorial();
            return;
        }

        var step = steps[currentStepIndex];

        if (tutorialOverlay != null) tutorialOverlay.style.display = DisplayStyle.Flex;
        if (tutorialHighlight != null) tutorialHighlight.style.display = DisplayStyle.Flex;
        if (tutorialArrow != null) tutorialArrow.style.display = DisplayStyle.Flex;
        if (tutorialTextPanel != null) tutorialTextPanel.style.display = DisplayStyle.Flex;

        if (tutorialTitle != null) tutorialTitle.text = step.title;
        if (tutorialBody != null) tutorialBody.text = step.description;

        VisualElement target = root.Q<VisualElement>(step.targetElementName);
        if (target == null)
        {
            Debug.LogWarning($"[UITutorialManager] Target '{step.targetElementName}' not found.");
            return;
        }

        PositionTutorialElements(target, step);

        if (tutorialNextButton != null)
            tutorialNextButton.text = (currentStepIndex >= steps.Count - 1) ? "Finish" : "Next";
    }

    void PositionTutorialElements(VisualElement target, UITutorialStep step)
    {
        Rect r = target.worldBound;

        float x = r.xMin - step.padding;
        float y = r.yMin - step.padding;
        float w = r.width + step.padding * 2f;
        float h = r.height + step.padding * 2f;

        tutorialHighlight.style.left = x;
        tutorialHighlight.style.top = y;
        tutorialHighlight.style.width = w;
        tutorialHighlight.style.height = h;

        string arrowChar = "↓";
        Vector2 arrowPos = new Vector2(r.center.x, r.yMin - 40f);

        switch (step.arrowDirection)
        {
            case TutorialArrowDirection.Up:
                arrowChar = "↑";
                arrowPos = new Vector2(r.center.x - 10f, r.yMax + 5f);
                break;

            case TutorialArrowDirection.Down:
                arrowChar = "↓";
                arrowPos = new Vector2(r.center.x - 10f, r.yMin - 45f);
                break;

            case TutorialArrowDirection.Left:
                arrowChar = "←";
                arrowPos = new Vector2(r.xMax + 5f, r.center.y - 20f);
                break;

            case TutorialArrowDirection.Right:
                arrowChar = "→";
                arrowPos = new Vector2(r.xMin - 35f, r.center.y - 20f);
                break;
        }

        arrowPos += step.arrowOffset;

        tutorialArrow.text = arrowChar;
        tutorialArrow.style.left = arrowPos.x;
        tutorialArrow.style.top = arrowPos.y;

        Vector2 panelPos = new Vector2(r.xMin, r.yMax) + step.textPanelOffset;

        tutorialTextPanel.style.left = panelPos.x;
        tutorialTextPanel.style.top = panelPos.y;
    }

    public void NextStep()
    {
        if (!tutorialOpen) return;

        currentStepIndex++;
        ShowCurrentStep();
    }
}