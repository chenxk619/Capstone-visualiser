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
            shieldObject = gameObject;

        HideShieldImmediate();
    }

    public void TriggerBlockShield()
    {
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
        if (shieldObject != null)
            shieldObject.SetActive(true);

        Debug.Log($"[RiotShieldController] Shield shown for {visibleDuration:F1}s.");

        yield return new WaitForSeconds(visibleDuration);

        if (shieldObject != null)
            shieldObject.SetActive(false);

        Debug.Log("[RiotShieldController] Shield hidden after timeout.");
        activeRoutine = null;
    }
}