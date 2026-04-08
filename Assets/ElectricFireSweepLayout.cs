using UnityEngine;

public class ElectricalFireSweepLayout : MonoBehaviour
{
    [Header("Assign in Left -> Middle -> Right order")]
    public GameObject[] smallFires = new GameObject[3];

    [Header("Layout")]
    public float scaleMultiplier = 0.5f;
    public float spacing = 0.35f;

    [Header("Optional vertical offset")]
    public float yOffset = 0f;
    public float zOffset = 0f;

    void Awake()
    {
        ApplyLayout();
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        ApplyLayout();
    }
#endif

    public void ApplyLayout()
    {
        if (smallFires == null || smallFires.Length == 0)
            return;

        for (int i = 0; i < smallFires.Length; i++)
        {
            if (smallFires[i] == null)
                continue;

            Transform t = smallFires[i].transform;

            t.localScale = Vector3.one * scaleMultiplier;

            float x = 0f;

            if (smallFires.Length == 3)
            {
                if (i == 0) x = -spacing;
                else if (i == 1) x = 0f;
                else x = spacing;
            }
            else
            {
                float centeredIndex = i - (smallFires.Length - 1) * 0.5f;
                x = centeredIndex * spacing;
            }

            t.localPosition = new Vector3(x, yOffset, zOffset);

            ElectricalSubFire subFire = smallFires[i].GetComponent<ElectricalSubFire>();
            if (subFire != null)
            {
                // force refresh of stored visible scale in editor/runtime
                // by disabling and re-enabling scale memory via SendMessage alternative not needed
            }
        }
    }
}