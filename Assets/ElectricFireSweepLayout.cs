using UnityEngine;

public class ElectricalFireSweepLayout : MonoBehaviour
{
    [Header("Assign in Left -> Middle -> Right order")]
    public GameObject[] smallFires = new GameObject[3];

    [Header("Layout")]
    public float scaleMultiplier = 0.5f;   // half size
    public float spacing = 0.35f;          // distance between centers

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

            // make each fire half the original size
            t.localScale = Vector3.one * scaleMultiplier;

            // arrange side by side: left, middle, right
            float x = 0f;

            if (smallFires.Length == 3)
            {
                if (i == 0) x = -spacing;
                else if (i == 1) x = 0f;
                else if (i == 2) x = spacing;
            }
            else
            {
                float centeredIndex = i - (smallFires.Length - 1) * 0.5f;
                x = centeredIndex * spacing;
            }

            t.localPosition = new Vector3(x, yOffset, zOffset);
        }
    }
}