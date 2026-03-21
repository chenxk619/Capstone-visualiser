using UnityEngine;

public class DoorBreachController : MonoBehaviour
{
    [Header("Door Root (optional)")]
    public GameObject doorRoot;

    [Header("Animator")]
    public Animator animator;

    [Header("Animator Trigger Name")]
    public string breachTriggerName = "Breach";

    [Header("One-time Breach")]
    public bool allowOnlyOnce = true;

    public bool hasBreached = false;

    void Start()
    {
        if (doorRoot == null)
            doorRoot = gameObject;

        if (animator == null)
            animator = GetComponent<Animator>();

        if (animator == null && doorRoot != null)
            animator = doorRoot.GetComponent<Animator>();

        if (animator == null)
            Debug.LogWarning($"[DoorBreachController] No Animator found on {gameObject.name}");
    }

    public void BreachDoor()
    {
        if (doorRoot != null && !doorRoot.activeSelf)
        {
            doorRoot.SetActive(true);
            Debug.Log("[DoorBreachController] Door root activated before breach.");
        }

        if (animator == null)
        {
            Debug.LogWarning("[DoorBreachController] Cannot breach: Animator is missing.");
            return;
        }

        if (allowOnlyOnce && hasBreached)
        {
            Debug.Log("[DoorBreachController] Door already breached.");
            return;
        }

        animator.ResetTrigger(breachTriggerName);
        animator.SetTrigger(breachTriggerName);
        hasBreached = true;

        Debug.Log("[DoorBreachController] Breach triggered.");
    }

    public void ResetBreach()
    {
        hasBreached = false;

        if (animator != null)
            animator.ResetTrigger(breachTriggerName);

        if (doorRoot != null)
            doorRoot.SetActive(true);

        Debug.Log("[DoorBreachController] Breach reset.");
    }
}