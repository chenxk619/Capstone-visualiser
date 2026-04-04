using UnityEngine;
using UnityEngine.Video;

[System.Serializable]
public class UITutorialStep
{
    public string title;

    [TextArea(3, 6)]
    public string description;

    public string targetElementName;

    public TutorialArrowDirection arrowDirection = TutorialArrowDirection.Down;

    public Vector2 textPanelOffset = new Vector2(0, 0);
    public Vector2 arrowOffset = new Vector2(0, 0);
    public float padding = 8f;

    [Header("Optional Media")]
    public Sprite tutorialImage;
    public VideoClip tutorialVideo;
}