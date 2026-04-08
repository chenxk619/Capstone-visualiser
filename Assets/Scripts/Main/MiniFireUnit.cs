using UnityEngine;
using System.Collections;

public class MiniFireUnit : MonoBehaviour
{
    [Header("Mini Fire")]
    public int miniIndex = -1; // 0=Left, 1=Middle, 2=Right
    public float extinguishDuration = 1f;

    [HideInInspector] public FireGroupController groupController;

    private Vector3 initialScale;
    private bool isExtinguishing = false;
    private bool isExtinguished = false;

    void Start()
    {
        initialScale = transform.localScale;
        if (initialScale == Vector3.zero)
            initialScale = Vector3.one;
    }

    void OnEnable()
    {
        if (initialScale == Vector3.zero)
            initialScale = transform.localScale == Vector3.zero ? Vector3.one : transform.localScale;

        transform.localScale = initialScale;
        isExtinguishing = false;
        isExtinguished = false;
    }

    public bool CanBeExtinguished()
    {
        return !isExtinguishing && !isExtinguished;
    }

    public void ResetMiniFire()
    {
        StopAllCoroutines();

        if (initialScale == Vector3.zero)
            initialScale = transform.localScale == Vector3.zero ? Vector3.one : transform.localScale;

        transform.localScale = initialScale;
        isExtinguishing = false;
        isExtinguished = false;
        gameObject.SetActive(true);
    }

    public void StartShrinkAndExtinguish()
    {
        if (!CanBeExtinguished())
            return;

        StartCoroutine(ShrinkRoutine());
    }

    IEnumerator ShrinkRoutine()
    {
        isExtinguishing = true;

        Vector3 startScale = transform.localScale;
        float t = 0f;

        while (t < extinguishDuration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / extinguishDuration);
            transform.localScale = Vector3.Lerp(startScale, Vector3.zero, k);
            yield return null;
        }

        transform.localScale = Vector3.zero;
        isExtinguishing = false;
        isExtinguished = true;
        gameObject.SetActive(false);

        if (groupController != null)
            groupController.OnMiniFireFullyExtinguished(this);
    }
}