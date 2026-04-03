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

    private Label fireNameLabel;

    /*
    Fire indexes:
    0 = Class A
    1 = Class B
    2 = Class C
    3 = Electrical
    4 = Class F

    Extinguisher indexes:
    0 = Foam
    1 = Water
    2 = Powder
    3 = Carbon Dioxide
    4 = Wet Chemical
    */

    void Start()
    {
        if (extinguisherModels == null || extinguisherModels.Length == 0)
        {
            Debug.LogWarning("[ExtinguisherModelSwitcher] No extinguisher models assigned.");
            return;
        }

        if (uiDocument != null)
        {
            var root = uiDocument.rootVisualElement;
            fireNameLabel = root.Q<Label>(labelName);

            if (fireNameLabel == null)
                Debug.LogWarning($"[ExtinguisherModelSwitcher] Label '{labelName}' not found.");
        }
        else
        {
            Debug.LogWarning("[ExtinguisherModelSwitcher] UIDocument not assigned.");
        }

        ShowOnly(currentIndex);
    }

    public void ShowOnly(int index)
    {
        if (extinguisherModels == null || extinguisherModels.Length == 0) return;
        if (index < 0 || index >= extinguisherModels.Length) return;

        currentIndex = index;

        for (int i = 0; i < extinguisherModels.Length; i++)
        {
            if (extinguisherModels[i] != null)
                extinguisherModels[i].SetActive(i == currentIndex);
        }

        UpdateLabel();
    }

    public void NextModel()
    {
        if (extinguisherModels == null || extinguisherModels.Length == 0) return;
        currentIndex = (currentIndex + 1) % extinguisherModels.Length;
        ShowOnly(currentIndex);
    }

    public void PreviousModel()
    {
        if (extinguisherModels == null || extinguisherModels.Length == 0) return;
        currentIndex--;
        if (currentIndex < 0) currentIndex = extinguisherModels.Length - 1;
        ShowOnly(currentIndex);
    }

    void UpdateLabel()
    {
        if (fireNameLabel == null) return;

        if (extinguisherNames != null &&
            currentIndex >= 0 &&
            currentIndex < extinguisherNames.Length)
        {
            fireNameLabel.text = extinguisherNames[currentIndex];
            Debug.Log($"[ExtinguisherModelSwitcher] Label updated to: {fireNameLabel.text}");
        }
        else
        {
            Debug.LogWarning("[ExtinguisherModelSwitcher] extinguisherNames is empty or index out of range.");
        }
    }

    public GameObject GetCurrentExtinguisher()
    {
        if (extinguisherModels == null || currentIndex < 0 || currentIndex >= extinguisherModels.Length)
            return null;

        return extinguisherModels[currentIndex];
    }

    public ExtinguisherExtinguish_CameraRay GetCurrentExtinguisherScript()
    {
        GameObject obj = GetCurrentExtinguisher();
        if (obj == null) return null;

        return obj.GetComponentInChildren<ExtinguisherExtinguish_CameraRay>(true);
    }

    public int GetCurrentIndex()
    {
        return currentIndex;
    }

    public int GetCurrentExtinguisherIndex()
    {
        return currentIndex;
    }

    public int[] GetAllowedFireIndexesForCurrentExtinguisher()
    {
        if (currentIndex == foamIndex)       // Foam
            return new int[] { 0, 1 };

        if (currentIndex == waterIndex)      // Water
            return new int[] { 0 };

        if (currentIndex == powderIndex)     // ABC Powder
            return new int[] { 0, 1, 2, 3 };

        if (currentIndex == carbonIndex)     // Carbon Dioxide
            return new int[] { 1, 3 };

        if (currentIndex == chemicalIndex)   // Wet Chemical
            return new int[] { 0, 4 };

        return new int[0];
    }

    public bool CanCurrentExtinguisherPutOutFire(int fireIndex)
    {
        int[] allowedFireIndexes = GetAllowedFireIndexesForCurrentExtinguisher();

        for (int i = 0; i < allowedFireIndexes.Length; i++)
        {
            if (allowedFireIndexes[i] == fireIndex)
                return true;
        }

        return false;
    }

    public void ShowFoam() => ShowOnly(foamIndex);
    public void ShowWater() => ShowOnly(waterIndex);
    public void ShowPowder() => ShowOnly(powderIndex);
    public void ShowCarbon() => ShowOnly(carbonIndex);
    public void ShowChemical() => ShowOnly(chemicalIndex);
}