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

    [Header("Fixed Index Mapping")]
    public int foamIndex = 0;
    public int waterIndex = 1;
    public int powderIndex = 2;
    public int carbonIndex = 3;
    public int chemicalIndex = 4;

    private Label nameLabel;

    void Start()
    {
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

        Debug.Log($"[ExtinguisherSwitcher] Showing index {currentIndex} -> {GetCurrentName()}");
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

        if (extinguisherModels != null &&
            currentIndex >= 0 &&
            currentIndex < extinguisherModels.Length &&
            extinguisherModels[currentIndex] != null)
        {
            return extinguisherModels[currentIndex].name;
        }

        return $"Extinguisher {currentIndex}";
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

    public void ShowFoam()
    {
        Debug.Log($"[ExtinguisherSwitcher] Select FOAM -> index {foamIndex}");
        ShowModel(foamIndex);
    }

    public void ShowWater()
    {
        Debug.Log($"[ExtinguisherSwitcher] Select WATER -> index {waterIndex}");
        ShowModel(waterIndex);
    }

    public void ShowPowder()
    {
        Debug.Log($"[ExtinguisherSwitcher] Select POWDER -> index {powderIndex}");
        ShowModel(powderIndex);
    }

    public void ShowCarbon()
    {
        Debug.Log($"[ExtinguisherSwitcher] Select CARBON -> index {carbonIndex}");
        ShowModel(carbonIndex);
    }

    public void ShowChemical()
    {
        Debug.Log($"[ExtinguisherSwitcher] Select CHEMICAL -> index {chemicalIndex}");
        ShowModel(chemicalIndex);
    }

    public ExtinguisherExtinguish_CameraRay GetCurrentExtinguisher()
    {
        if (extinguisherModels == null || extinguisherModels.Length == 0)
            return null;

        GameObject current = extinguisherModels[currentIndex];
        if (current == null) return null;

        return current.GetComponentInChildren<ExtinguisherExtinguish_CameraRay>(true);
    }
}