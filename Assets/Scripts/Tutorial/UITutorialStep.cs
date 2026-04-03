using UnityEngine;

[System.Serializable]
public class UITutorialStep
{
    public string title;

    [TextArea(3, 6)]
    public string description;

    public string targetElementName;

    public TutorialArrowDirection arrowDirection = TutorialArrowDirection.Down;

    public Vector2 textPanelOffset = new Vector2(0, 120);
    public Vector2 arrowOffset = new Vector2(0, -50);
    public float padding = 10f;
}