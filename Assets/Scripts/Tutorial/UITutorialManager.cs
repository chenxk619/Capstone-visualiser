using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class UITutorialManager : MonoBehaviour
{
    [Header("UI")]
    public UIDocument uiDocument;

    [Header("Tutorial Steps")]
    public List<UITutorialStep> steps = new List<UITutorialStep>();

    private VisualElement root;
    private VisualElement tutorialOverlay;

    private VisualElement tutorialMaskTop;
    private VisualElement tutorialMaskBottom;
    private VisualElement tutorialMaskLeft;
    private VisualElement tutorialMaskRight;

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

        tutorialMaskTop = root.Q<VisualElement>("TutorialMaskTop");
        tutorialMaskBottom = root.Q<VisualElement>("TutorialMaskBottom");
        tutorialMaskLeft = root.Q<VisualElement>("TutorialMaskLeft");
        tutorialMaskRight = root.Q<VisualElement>("TutorialMaskRight");

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

        Invoke(nameof(ShowCurrentStep), 0.05f);
    }

    public void CloseTutorial()
    {
        tutorialOpen = false;
        HideTutorial();
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
        if (tutorialHighlight == null || tutorialArrow == null || tutorialTextPanel == null)
        {
            Debug.LogWarning("[UITutorialManager] Missing tutorial UI references.");
            return;
        }

        Rect r = target.worldBound;
        Rect rootRect = root.worldBound;

        float padding = Mathf.Max(4f, step.padding);

        float holeX = r.xMin - padding;
        float holeY = r.yMin - padding;
        float holeW = r.width + padding * 2f;
        float holeH = r.height + padding * 2f;

        tutorialHighlight.style.left = holeX;
        tutorialHighlight.style.top = holeY;
        tutorialHighlight.style.width = holeW;
        tutorialHighlight.style.height = holeH;

        PositionMasks(rootRect, holeX, holeY, holeW, holeH);
        PositionArrow(r, rootRect, step);
        PositionTextPanel(r, rootRect, step);
        PositionNextButton(rootRect);
    }

    void PositionMasks(Rect rootRect, float holeX, float holeY, float holeW, float holeH)
    {
        if (tutorialMaskTop != null)
        {
            tutorialMaskTop.style.left = 0;
            tutorialMaskTop.style.top = 0;
            tutorialMaskTop.style.width = rootRect.width;
            tutorialMaskTop.style.height = holeY;
        }

        if (tutorialMaskBottom != null)
        {
            tutorialMaskBottom.style.left = 0;
            tutorialMaskBottom.style.top = holeY + holeH;
            tutorialMaskBottom.style.width = rootRect.width;
            tutorialMaskBottom.style.height = Mathf.Max(0, rootRect.height - (holeY + holeH));
        }

        if (tutorialMaskLeft != null)
        {
            tutorialMaskLeft.style.left = 0;
            tutorialMaskLeft.style.top = holeY;
            tutorialMaskLeft.style.width = holeX;
            tutorialMaskLeft.style.height = holeH;
        }

        if (tutorialMaskRight != null)
        {
            tutorialMaskRight.style.left = holeX + holeW;
            tutorialMaskRight.style.top = holeY;
            tutorialMaskRight.style.width = Mathf.Max(0, rootRect.width - (holeX + holeW));
            tutorialMaskRight.style.height = holeH;
        }
    }

    void PositionArrow(Rect r, Rect rootRect, UITutorialStep step)
    {
        string arrowChar = "↓";
        Vector2 arrowPos = new Vector2(r.center.x - 10f, r.yMin - 28f);

        switch (step.arrowDirection)
        {
            case TutorialArrowDirection.Up:
                arrowChar = "↑";
                arrowPos = new Vector2(r.center.x - 10f, r.yMax + 2f);
                break;

            case TutorialArrowDirection.Down:
                arrowChar = "↓";
                arrowPos = new Vector2(r.center.x - 10f, r.yMin - 28f);
                break;

            case TutorialArrowDirection.Left:
                arrowChar = "←";
                arrowPos = new Vector2(r.xMax + 2f, r.center.y - 14f);
                break;

            case TutorialArrowDirection.Right:
                arrowChar = "→";
                arrowPos = new Vector2(r.xMin - 24f, r.center.y - 14f);
                break;
        }

        arrowPos += step.arrowOffset;

        tutorialArrow.text = arrowChar;
        tutorialArrow.style.left = Mathf.Clamp(arrowPos.x, 0f, rootRect.width - 24f);
        tutorialArrow.style.top = Mathf.Clamp(arrowPos.y, 0f, rootRect.height - 24f);
    }

    void PositionTextPanel(Rect r, Rect rootRect, UITutorialStep step)
    {
        float panelWidth = 220f;
        float panelHeight = 110f;

        float x = (rootRect.width - panelWidth) * 0.5f;
        float y = (rootRect.height - panelHeight) * 0.5f;

        tutorialTextPanel.style.width = panelWidth;
        tutorialTextPanel.style.minHeight = panelHeight;
        tutorialTextPanel.style.left = x;
        tutorialTextPanel.style.top = y;
    }

    void PositionNextButton(Rect rootRect)
    {
        if (tutorialNextButton == null)
            return;

        float buttonWidth = 80f;
        float buttonHeight = 30f;

        float x = rootRect.width - buttonWidth - 12f;
        float y = (rootRect.height - buttonHeight) * 0.5f;

        tutorialNextButton.style.position = Position.Absolute;
        tutorialNextButton.style.width = buttonWidth;
        tutorialNextButton.style.height = buttonHeight;
        tutorialNextButton.style.left = x;
        tutorialNextButton.style.top = y;
    }

    public void NextStep()
    {
        if (!tutorialOpen) return;

        currentStepIndex++;
        ShowCurrentStep();
    }
}