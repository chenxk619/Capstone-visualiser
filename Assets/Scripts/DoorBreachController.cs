using UnityEngine;

public class DoorBreachController : MonoBehaviour
{
    [Header("Animator")]
    public Animator animator;

    [Header("Animator Trigger Name")]
    public string breachTriggerName = "Breach";

    [Header("One-time Breach")]
    public bool allowOnlyOnce = true;

    public bool hasBreached = false;

    void Start()
    {
        if (animator == null)
            animator = GetComponent<Animator>();

        if (animator == null)
            Debug.LogWarning($"[DoorBreachController] No Animator found on {gameObject.name}");
    }

    public void BreachDoor()
    {
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

        animator.SetTrigger(breachTriggerName);
        hasBreached = true;

        Debug.Log("[DoorBreachController] Breach triggered.");
    }

    public void ResetBreach()
    {
        hasBreached = false;
        Debug.Log("[DoorBreachController] Breach reset.");
    }
}