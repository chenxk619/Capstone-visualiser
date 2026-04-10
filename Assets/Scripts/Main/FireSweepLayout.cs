using UnityEngine;

public class FireSweepLayout : MonoBehaviour
{
    [Header("Assign in Left -> Middle -> Right order")]
    public GameObject[] miniFires = new GameObject[3];

    [Header("Layout")]
    public float scaleMultiplier = 0.5f;
    public float spacing = 0.35f;

    [Header("Optional Offset")]
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
        if (miniFires == null || miniFires.Length == 0)
            return;

        for (int i = 0; i < miniFires.Length; i++)
        {
            if (miniFires[i] == null)
                continue;

            Transform t = miniFires[i].transform;

            t.localScale = Vector3.one * scaleMultiplier;

            float x = 0f;

            if (miniFires.Length == 3)
            {
                if (i == 0) x = -spacing;
                else if (i == 1) x = 0f;
                else x = spacing;
            }
            else
            {
                float centeredIndex = i - (miniFires.Length - 1) * 0.5f;
                x = centeredIndex * spacing;
            }

            t.localPosition = new Vector3(x, yOffset, zOffset);
        }
    }
}