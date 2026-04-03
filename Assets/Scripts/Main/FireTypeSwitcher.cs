using UnityEngine;

public class FireTypeSwitcher : MonoBehaviour
{
    [Header("Optional parent root")]
    public GameObject fireRoot;

    [Header("Fire Variants")]
    public GameObject foamFire;
    public GameObject waterFire;
    public GameObject powderFire;
    public GameObject carbonFire;
    public GameObject chemicalFire;

    public int CurrentFireIndex { get; private set; } = -1;

    void Awake()
    {
        if (fireRoot != null)
            fireRoot.SetActive(false);

        HideAll();
    }

    public void ShowFireByIndex(int index)
    {
        CurrentFireIndex = index;

        if (fireRoot != null)
            fireRoot.SetActive(true);

        if (foamFire != null) foamFire.SetActive(index == 0);
        if (waterFire != null) waterFire.SetActive(index == 1);
        if (powderFire != null) powderFire.SetActive(index == 2);
        if (carbonFire != null) carbonFire.SetActive(index == 3);
        if (chemicalFire != null) chemicalFire.SetActive(index == 4);
    }

    public void HideAll()
    {
        CurrentFireIndex = -1;

        if (foamFire != null) foamFire.SetActive(false);
        if (waterFire != null) waterFire.SetActive(false);
        if (powderFire != null) powderFire.SetActive(false);
        if (carbonFire != null) carbonFire.SetActive(false);
        if (chemicalFire != null) chemicalFire.SetActive(false);
    }

    public void HideEverything()
    {
        HideAll();

        if (fireRoot != null)
            fireRoot.SetActive(false);
    }
}