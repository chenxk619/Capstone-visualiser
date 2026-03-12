using UnityEngine;
using UnityEngine.UIElements;

public class ExtinguisherModelSwitcher : MonoBehaviour
{
    [Header("Drag all extinguisher model roots here")]
    public GameObject[] extinguisherModels;

    [Header("Display Names")]
    public string[] extinguisherNames;

    [Header("UI")]
    public UIDocument uiDocument;
    public string labelName = "FireExtinguisherName";

    [Header("Starting model index")]
    public int currentIndex = 0;

    private Label nameLabel;

    void Start()
    {
        // Find label in UI Toolkit
        if (uiDocument != null)
        {
            var root = uiDocument.rootVisualElement;
            nameLabel = root.Q<Label>(labelName);
        }

        ShowModel(currentIndex);
    }

    public void ShowModel(int index)
    {
        if (extinguisherModels == null || extinguisherModels.Length == 0)
            return;

        if (index < 0) index = 0;
        if (index >= extinguisherModels.Length)
            index = extinguisherModels.Length - 1;

        for (int i = 0; i < extinguisherModels.Length; i++)
        {
            if (extinguisherModels[i] != null)
            {
                bool active = (i == index);
                extinguisherModels[i].SetActive(active);
            }
        }

        currentIndex = index;

        UpdateUILabel();

        Debug.Log($"[ExtinguisherSwitcher] Showing {GetCurrentName()}");
    }

    void UpdateUILabel()
    {
        if (nameLabel == null) return;

        nameLabel.text = GetCurrentName();
    }

    string GetCurrentName()
    {
        if (extinguisherNames != null && currentIndex < extinguisherNames.Length)
            return extinguisherNames[currentIndex];

        return extinguisherModels[currentIndex].name;
    }

    public void NextModel()
    {
        if (extinguisherModels == null || extinguisherModels.Length == 0)
            return;

        currentIndex++;
        if (currentIndex >= extinguisherModels.Length)
            currentIndex = 0;

        ShowModel(currentIndex);
    }

    public void PreviousModel()
    {
        if (extinguisherModels == null || extinguisherModels.Length == 0)
            return;

        currentIndex--;
        if (currentIndex < 0)
            currentIndex = extinguisherModels.Length - 1;

        ShowModel(currentIndex);
    }

    public ExtinguisherExtinguish_CameraRay GetCurrentExtinguisher()
    {
        if (extinguisherModels == null || extinguisherModels.Length == 0)
            return null;

        GameObject current = extinguisherModels[currentIndex];

        return current.GetComponentInChildren<ExtinguisherExtinguish_CameraRay>(true);
    }
}