using UnityEngine;

public class MiniFireUnit : MonoBehaviour
{
    [Header("Mini Fire")]
    public int miniIndex = -1; // 0=Left, 1=Middle, 2=Right
    public float extinguishDuration = 1f;

    [Header("Visual Shrink")]
    [Range(0f, 1f)]
    public float minimumScaleMultiplier = 0.15f;

    [HideInInspector] public FireGroupController groupController;

    private Vector3 initialScale;
    private float currentProgress = 0f;
    private bool isExtinguished = false;

    void Awake()
    {
        CacheInitialScale();
    }

    void Start()
    {
        CacheInitialScale();
    }

    void OnEnable()
    {
        CacheInitialScale();

        if (!isExtinguished)
            UpdateVisualScale();
    }

    void CacheInitialScale()
    {
        if (initialScale == Vector3.zero)
        {
            initialScale = transform.localScale;
            if (initialScale == Vector3.zero)
                initialScale = Vector3.one;
        }
    }

    public bool CanBeExtinguished()
    {
        return !isExtinguished;
    }

    public void ResetMiniFire()
    {
        CacheInitialScale();

        currentProgress = 0f;
        isExtinguished = false;
        transform.localScale = initialScale;
        gameObject.SetActive(true);
    }

    public void ShowMiniFire()
    {
        if (!isExtinguished)
            gameObject.SetActive(true);
    }

    public void AddSprayProgress(float deltaTimeAmount)
    {
        if (isExtinguished)
            return;

        if (extinguishDuration <= 0f)
            extinguishDuration = 0.01f;

        currentProgress += deltaTimeAmount;
        currentProgress = Mathf.Clamp(currentProgress, 0f, extinguishDuration);

        UpdateVisualScale();

        if (currentProgress >= extinguishDuration)
        {
            FullyExtinguish();
        }
    }

    void UpdateVisualScale()
    {
        float t = Mathf.Clamp01(currentProgress / extinguishDuration);
        float scaleMultiplier = Mathf.Lerp(1f, minimumScaleMultiplier, t);
        transform.localScale = initialScale * scaleMultiplier;
    }

    void FullyExtinguish()
    {
        if (isExtinguished)
            return;

        isExtinguished = true;
        transform.localScale = Vector3.zero;
        gameObject.SetActive(false);

        if (groupController != null)
            groupController.OnMiniFireFullyExtinguished(this);
    }

    public float GetProgress01()
    {
        if (extinguishDuration <= 0f) return 1f;
        return Mathf.Clamp01(currentProgress / extinguishDuration);
    }

    public bool IsExtinguished()
    {
        return isExtinguished;
    }
}