using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

public class TutorialManager : MonoBehaviour
{
    [Header("UI")]
    public UIDocument uiDocument;
    public string tutorialPanelName = "TutorialPanel";
    public string titleLabelName = "TutorialTitle";
    public string bodyLabelName = "TutorialBody";
    public string nextButtonName = "TutorialNextButton";
    public string prevButtonName = "TutorialPrevButton";
    public string closeButtonName = "TutorialCloseButton";

    [Header("Slides")]
    public List<TutorialStep> steps = new List<TutorialStep>();

    private VisualElement tutorialPanel;
    private Label titleLabel;
    private Label bodyLabel;
    private Button nextButton;
    private Button prevButton;
    private Button closeButton;

    private int currentIndex = 0;
    private bool tutorialOpen = false;

    void Start()
    {
        if (uiDocument == null)
        {
            Debug.LogWarning("[TutorialManager] UIDocument is not assigned.");
            return;
        }

        var root = uiDocument.rootVisualElement;

        tutorialPanel = root.Q<VisualElement>(tutorialPanelName);
        titleLabel = root.Q<Label>(titleLabelName);
        bodyLabel = root.Q<Label>(bodyLabelName);
        nextButton = root.Q<Button>(nextButtonName);
        prevButton = root.Q<Button>(prevButtonName);
        closeButton = root.Q<Button>(closeButtonName);

        if (nextButton != null)
            nextButton.clicked += NextSlide;
        else
            Debug.LogWarning($"[TutorialManager] Button '{nextButtonName}' not found.");

        if (prevButton != null)
            prevButton.clicked += PreviousSlide;
        else
            Debug.LogWarning($"[TutorialManager] Button '{prevButtonName}' not found.");

        if (closeButton != null)
            closeButton.clicked += CloseTutorial;
        else
            Debug.LogWarning($"[TutorialManager] Button '{closeButtonName}' not found.");

        HideTutorial();
    }

    public void OpenTutorial()
    {
        if (steps == null || steps.Count == 0)
        {
            Debug.LogWarning("[TutorialManager] No tutorial steps assigned.");
            return;
        }

        tutorialOpen = true;
        currentIndex = 0;

        if (tutorialPanel != null)
            tutorialPanel.style.display = DisplayStyle.Flex;

        ShowCurrentSlide();

        Debug.Log("[TutorialManager] Tutorial opened.");
    }

    public void CloseTutorial()
    {
        tutorialOpen = false;
        HideTutorial();

        Debug.Log("[TutorialManager] Tutorial closed.");
    }

    void HideTutorial()
    {
        if (tutorialPanel != null)
            tutorialPanel.style.display = DisplayStyle.None;
    }

    void ShowCurrentSlide()
    {
        if (!tutorialOpen) return;
        if (steps == null || steps.Count == 0) return;
        if (currentIndex < 0 || currentIndex >= steps.Count) return;

        TutorialStep step = steps[currentIndex];

        if (titleLabel != null)
            titleLabel.text = step.title;

        if (bodyLabel != null)
            bodyLabel.text = step.description;

        if (prevButton != null)
            prevButton.SetEnabled(currentIndex > 0);

        if (nextButton != null)
            nextButton.text = (currentIndex >= steps.Count - 1) ? "Finish" : "Next";
    }

    public void NextSlide()
    {
        if (!tutorialOpen) return;

        if (currentIndex < steps.Count - 1)
        {
            currentIndex++;
            ShowCurrentSlide();
        }
        else
        {
            CloseTutorial();
        }
    }

    public void PreviousSlide()
    {
        if (!tutorialOpen) return;

        if (currentIndex > 0)
        {
            currentIndex--;
            ShowCurrentSlide();
        }
    }
}