using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Video;

public class UITutorialManager : MonoBehaviour
{
    [Header("UI")]
    public UIDocument uiDocument;

    [Header("Start Screen")]
    public GameObject startPanelCanvas;

    [Header("Tutorial Steps")]
    public List<UITutorialStep> steps = new List<UITutorialStep>();

    [Header("Optional Video")]
    public VideoPlayer tutorialVideoPlayer;
    public RenderTexture tutorialRenderTexture;

    private VisualElement root;
    private VisualElement tutorialOverlay;

    private VisualElement tutorialMaskTop;
    private VisualElement tutorialMaskBottom;
    private VisualElement tutorialMaskLeft;
    private VisualElement tutorialMaskRight;

    private VisualElement tutorialHighlight;

    private VisualElement tutorialTextPanel;
    private Label tutorialTitle;
    private Label tutorialBody;
    private VisualElement tutorialMediaImage;

    private Button tutorialNextButton;
    private Button tutorialPrevButton;

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

        tutorialTextPanel = root.Q<VisualElement>("TutorialTextPanel");
        tutorialTitle = root.Q<Label>("TutorialTitle");
        tutorialBody = root.Q<Label>("TutorialBody");
        tutorialMediaImage = root.Q<VisualElement>("TutorialMediaImage");

        tutorialNextButton = root.Q<Button>("TutorialNextButton");
        tutorialPrevButton = root.Q<Button>("TutorialPrevButton");

        if (tutorialNextButton != null)
            tutorialNextButton.clicked += NextStep;
        else
            Debug.LogWarning("[UITutorialManager] TutorialNextButton not found.");

        if (tutorialPrevButton != null)
            tutorialPrevButton.clicked += PrevStep;
        else
            Debug.LogWarning("[UITutorialManager] TutorialPrevButton not found.");

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

        if (startPanelCanvas != null)
            startPanelCanvas.SetActive(false);

        Invoke(nameof(ShowCurrentStep), 0.05f);
    }

    public void CloseTutorial()
    {
        tutorialOpen = false;

        if (tutorialVideoPlayer != null)
        {
            tutorialVideoPlayer.prepareCompleted -= OnTutorialVideoPrepared;

            if (tutorialVideoPlayer.isPlaying)
                tutorialVideoPlayer.Stop();
        }

        HideTutorial();

        if (startPanelCanvas != null)
            startPanelCanvas.SetActive(true);
    }

    void HideTutorial()
    {
        if (tutorialOverlay != null) tutorialOverlay.style.display = DisplayStyle.None;
        if (tutorialHighlight != null) tutorialHighlight.style.display = DisplayStyle.None;
        if (tutorialTextPanel != null) tutorialTextPanel.style.display = DisplayStyle.None;
        if (tutorialNextButton != null) tutorialNextButton.style.display = DisplayStyle.None;
        if (tutorialPrevButton != null) tutorialPrevButton.style.display = DisplayStyle.None;
    }

    void ShowCurrentStep()
    {
        if (!tutorialOpen || steps == null || steps.Count == 0)
            return;

        if (currentStepIndex < 0)
            currentStepIndex = 0;

        if (currentStepIndex >= steps.Count)
        {
            CloseTutorial();
            return;
        }

        UITutorialStep step = steps[currentStepIndex];

        if (tutorialOverlay != null) tutorialOverlay.style.display = DisplayStyle.Flex;
        if (tutorialHighlight != null) tutorialHighlight.style.display = DisplayStyle.Flex;
        if (tutorialTextPanel != null) tutorialTextPanel.style.display = DisplayStyle.Flex;
        if (tutorialNextButton != null) tutorialNextButton.style.display = DisplayStyle.Flex;
        if (tutorialPrevButton != null) tutorialPrevButton.style.display = DisplayStyle.Flex;

        bool hasTitle = !string.IsNullOrWhiteSpace(step.title);
        bool hasBody = !string.IsNullOrWhiteSpace(step.description);
        bool hasAnyText = hasTitle || hasBody;

        if (tutorialTitle != null)
        {
            tutorialTitle.text = hasTitle ? step.title : "";
            tutorialTitle.style.display = hasTitle ? DisplayStyle.Flex : DisplayStyle.None;
        }

        if (tutorialBody != null)
        {
            tutorialBody.text = hasBody ? step.description : "";
            tutorialBody.style.display = hasBody ? DisplayStyle.Flex : DisplayStyle.None;
        }

        if (tutorialMediaImage != null)
        {
            bool showingMedia = false;

            if (step.tutorialVideo != null && tutorialVideoPlayer != null && tutorialRenderTexture != null)
            {
                tutorialVideoPlayer.Stop();
                tutorialVideoPlayer.clip = step.tutorialVideo;
                tutorialVideoPlayer.renderMode = VideoRenderMode.RenderTexture;
                tutorialVideoPlayer.targetTexture = tutorialRenderTexture;
                tutorialVideoPlayer.isLooping = true;
                tutorialVideoPlayer.waitForFirstFrame = true;

                tutorialVideoPlayer.prepareCompleted -= OnTutorialVideoPrepared;
                tutorialVideoPlayer.prepareCompleted += OnTutorialVideoPrepared;
                tutorialVideoPlayer.Prepare();

                tutorialMediaImage.style.display = DisplayStyle.Flex;
                tutorialMediaImage.style.backgroundImage =
                    new StyleBackground(Background.FromRenderTexture(tutorialRenderTexture));

                showingMedia = true;
            }
            else if (step.tutorialImage != null)
            {
                if (tutorialVideoPlayer != null)
                {
                    tutorialVideoPlayer.prepareCompleted -= OnTutorialVideoPrepared;
                    if (tutorialVideoPlayer.isPlaying)
                        tutorialVideoPlayer.Stop();
                }

                tutorialMediaImage.style.display = DisplayStyle.Flex;
                tutorialMediaImage.style.backgroundImage = new StyleBackground(step.tutorialImage);
                showingMedia = true;
            }
            else
            {
                if (tutorialVideoPlayer != null)
                {
                    tutorialVideoPlayer.prepareCompleted -= OnTutorialVideoPrepared;
                    if (tutorialVideoPlayer.isPlaying)
                        tutorialVideoPlayer.Stop();
                }

                tutorialMediaImage.style.backgroundImage = StyleKeyword.None;
            }

            if (!showingMedia)
                tutorialMediaImage.style.display = DisplayStyle.None;
        }

        UpdatePanelLayout(hasAnyText);

        VisualElement target = root.Q<VisualElement>(step.targetElementName);
        if (target == null)
        {
            Debug.LogWarning($"[UITutorialManager] Target '{step.targetElementName}' not found.");
            return;
        }

        PositionTutorialElements(target, step);

        if (tutorialNextButton != null)
            tutorialNextButton.text = "";

        if (tutorialPrevButton != null)
        {
            tutorialPrevButton.text = "";
            tutorialPrevButton.SetEnabled(currentStepIndex > 0);
        }
    }

    void UpdatePanelLayout(bool hasAnyText)
    {
        if (tutorialMediaImage == null)
            return;

        if (hasAnyText)
        {
            tutorialMediaImage.style.height = 90f;
            tutorialMediaImage.style.flexGrow = 0f;
        }
        else
        {
            tutorialMediaImage.style.height = StyleKeyword.Auto;
            tutorialMediaImage.style.flexGrow = 1f;
        }
    }

    void PositionTutorialElements(VisualElement target, UITutorialStep step)
    {
        if (root == null || tutorialHighlight == null || tutorialTextPanel == null || tutorialNextButton == null)
            return;

        Rect r = target.worldBound;
        Rect rootRect = root.worldBound;

        float padding = Mathf.Max(8f, step.padding);

        float holeX = r.xMin - padding;
        float holeY = r.yMin - padding;
        float holeW = r.width + padding * 2f;
        float holeH = r.height + padding * 2f;

        tutorialHighlight.style.left = holeX;
        tutorialHighlight.style.top = holeY;
        tutorialHighlight.style.width = holeW;
        tutorialHighlight.style.height = holeH;

        PositionMasks(rootRect, holeX, holeY, holeW, holeH);
        PositionTextPanel(rootRect);
        PositionNextButton(rootRect);
        PositionPrevButton(rootRect);
    }

    void PositionMasks(Rect rootRect, float holeX, float holeY, float holeW, float holeH)
    {
        if (tutorialMaskTop != null)
        {
            tutorialMaskTop.style.left = 0;
            tutorialMaskTop.style.top = 0;
            tutorialMaskTop.style.width = rootRect.width;
            tutorialMaskTop.style.height = Mathf.Max(0f, holeY);
        }

        if (tutorialMaskBottom != null)
        {
            tutorialMaskBottom.style.left = 0;
            tutorialMaskBottom.style.top = holeY + holeH;
            tutorialMaskBottom.style.width = rootRect.width;
            tutorialMaskBottom.style.height = Mathf.Max(0f, rootRect.height - (holeY + holeH));
        }

        if (tutorialMaskLeft != null)
        {
            tutorialMaskLeft.style.left = 0;
            tutorialMaskLeft.style.top = holeY;
            tutorialMaskLeft.style.width = Mathf.Max(0f, holeX);
            tutorialMaskLeft.style.height = holeH;
        }

        if (tutorialMaskRight != null)
        {
            tutorialMaskRight.style.left = holeX + holeW;
            tutorialMaskRight.style.top = holeY;
            tutorialMaskRight.style.width = Mathf.Max(0f, rootRect.width - (holeX + holeW));
            tutorialMaskRight.style.height = holeH;
        }
    }

    void PositionTextPanel(Rect rootRect)
    {
        if (tutorialTextPanel == null)
            return;

        float panelWidth = 300f;
        float panelHeight = 210f;

        float x = (rootRect.width - panelWidth) * 0.5f;
        float y = (rootRect.height - panelHeight) * 0.5f;

        tutorialTextPanel.style.width = panelWidth;
        tutorialTextPanel.style.height = panelHeight;
        tutorialTextPanel.style.left = x;
        tutorialTextPanel.style.top = y;
    }

    void PositionNextButton(Rect rootRect)
    {
        if (tutorialNextButton == null)
            return;

        float buttonWidth = 130f;
        float buttonHeight = 60f;

        float x = rootRect.width - buttonWidth - 12f;
        float y = (rootRect.height - buttonHeight) * 0.5f;

        tutorialNextButton.style.position = Position.Absolute;
        tutorialNextButton.style.width = buttonWidth;
        tutorialNextButton.style.height = buttonHeight;
        tutorialNextButton.style.left = x;
        tutorialNextButton.style.top = y;
    }

    void PositionPrevButton(Rect rootRect)
    {
        if (tutorialPrevButton == null)
            return;

        float buttonWidth = 130f;
        float buttonHeight = 60f;

        float x = 12f;
        float y = (rootRect.height - buttonHeight) * 0.5f;

        tutorialPrevButton.style.position = Position.Absolute;
        tutorialPrevButton.style.width = buttonWidth;
        tutorialPrevButton.style.height = buttonHeight;
        tutorialPrevButton.style.left = x;
        tutorialPrevButton.style.top = y;
    }

    void OnTutorialVideoPrepared(VideoPlayer vp)
    {
        vp.Play();
    }

    public void NextStep()
    {
        if (!tutorialOpen)
            return;

        currentStepIndex++;
        ShowCurrentStep();
    }

    public void PrevStep()
    {
        if (!tutorialOpen)
            return;

        currentStepIndex--;
        if (currentStepIndex < 0)
            currentStepIndex = 0;

        ShowCurrentStep();
    }
}