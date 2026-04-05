using UnityEngine;
using UnityEngine.Video;

[System.Serializable]
public class UITutorialStep
{
    public string title;

    [TextArea(3, 6)]
    public string description;

    public string targetElementName;

    public float padding = 8f;

    [Header("Optional Media")]
    public Texture2D tutorialImage;
    public VideoClip tutorialVideo;
}