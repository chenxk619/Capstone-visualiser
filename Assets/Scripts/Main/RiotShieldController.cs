using System.Collections;
using UnityEngine;

public class RiotShieldController : MonoBehaviour
{
    [Header("Shield Object")]
    public GameObject shieldObject;

    [Header("Timing")]
    public float visibleDuration = 3f;

    private Coroutine activeRoutine;

    private void Awake()
    {
        if (shieldObject == null)
        {
            Debug.LogWarning("[RiotShieldController] shieldObject is not assigned.");
            return;
        }

        shieldObject.SetActive(false);
    }

    public void TriggerBlockShield()
    {
        if (shieldObject == null)
        {
            Debug.LogWarning("[RiotShieldController] Cannot trigger shield: shieldObject is null.");
            return;
        }

        if (activeRoutine != null)
            StopCoroutine(activeRoutine);

        activeRoutine = StartCoroutine(ShowShieldTemporarily());
    }

    public void HideShieldImmediate()
    {
        if (activeRoutine != null)
        {
            StopCoroutine(activeRoutine);
            activeRoutine = null;
        }

        if (shieldObject != null)
            shieldObject.SetActive(false);

        Debug.Log("[RiotShieldController] Shield hidden.");
    }

    private IEnumerator ShowShieldTemporarily()
    {
        shieldObject.SetActive(true);
        Debug.Log($"[RiotShieldController] Shield shown for {visibleDuration:F1}s.");

        yield return new WaitForSeconds(visibleDuration);

        shieldObject.SetActive(false);
        Debug.Log("[RiotShieldController] Shield hidden after timeout.");
        activeRoutine = null;
    }
}