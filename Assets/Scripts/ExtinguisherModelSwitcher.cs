using UnityEngine;

public class ExtinguisherModelSwitcher : MonoBehaviour
{
    [Header("Drag all extinguisher model roots here")]
    public GameObject[] extinguisherModels;

    [Header("Starting model index")]
    public int currentIndex = 0;

    void Start()
    {
        ShowModel(currentIndex);
    }

    public void ShowModel(int index)
    {
        if (extinguisherModels == null || extinguisherModels.Length == 0)
            return;

        if (index < 0) index = 0;
        if (index >= extinguisherModels.Length) index = extinguisherModels.Length - 1;

        for (int i = 0; i < extinguisherModels.Length; i++)
        {
            if (extinguisherModels[i] != null)
                extinguisherModels[i].SetActive(i == index);
        }

        currentIndex = index;
        Debug.Log($"[ExtinguisherSwitcher] Showing model {currentIndex}");
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
}