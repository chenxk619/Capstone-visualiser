using UnityEngine;
using Vuforia;

public class TrackedFireTarget : MonoBehaviour
{
    [Header("Fire Info")]
    public int fireIndex = -1;   // 0=A, 1=B, 2=C, 3=Electrical, 4=F
    public GameObject fireObject;

    [Header("Manager")]
    public FireChallengeManager challengeManager;

    private ObserverBehaviour observer;
    private bool permanentlyExtinguished = false;

    void Start()
    {
        observer = GetComponent<ObserverBehaviour>();

        if (fireObject != null)
            fireObject.SetActive(false);

        if (observer != null)
            observer.OnTargetStatusChanged += OnTargetStatusChanged;
        else
            Debug.LogWarning($"[TrackedFireTarget] No ObserverBehaviour found on {gameObject.name}");
    }

    void OnDestroy()
    {
        if (observer != null)
            observer.OnTargetStatusChanged -= OnTargetStatusChanged;
    }

    void OnTargetStatusChanged(ObserverBehaviour behaviour, TargetStatus status)
    {
        // Use only TRACKED so fire disappears immediately when QR is lost
        bool tracked = status.Status == Status.TRACKED ||
        status.Status == Status.EXTENDED_TRACKED;

        if (fireObject != null)
        {
            if (permanentlyExtinguished)
                fireObject.SetActive(false);
            else
                fireObject.SetActive(tracked);
        }

        if (challengeManager != null)
        {
            if (tracked && !permanentlyExtinguished)
                challengeManager.SetTrackedFire(fireIndex, fireObject);
            else
                challengeManager.ClearTrackedFire(fireIndex);
        }
    }

    public void MarkExtinguished()
    {
        permanentlyExtinguished = true;

        if (fireObject != null)
            fireObject.SetActive(false);
    }

    public void ResetTrackedFire()
    {
        permanentlyExtinguished = false;

        if (fireObject != null)
            fireObject.SetActive(false);
    }
}