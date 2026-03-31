using UnityEngine;
using Vuforia;

public class TrackedFireTarget : MonoBehaviour
{
    [Header("Fire Info")]
    public int fireIndex = -1;
    public GameObject fireObject;

    [Header("Manager")]
    public FireChallengeManager challengeManager;

    private ObserverBehaviour observer;

    void Start()
    {
        observer = GetComponent<ObserverBehaviour>();

        if (fireObject != null)
            fireObject.SetActive(false);

        if (observer != null)
            observer.OnTargetStatusChanged += OnTargetStatusChanged;
    }

    void OnDestroy()
    {
        if (observer != null)
            observer.OnTargetStatusChanged -= OnTargetStatusChanged;
    }

    void OnTargetStatusChanged(ObserverBehaviour behaviour, TargetStatus status)
    {
        bool tracked = status.Status == Status.TRACKED;

        Debug.Log($"[TrackedFireTarget] {gameObject.name} status={status.Status}, info={status.StatusInfo}, tracked={tracked}");

        if (fireObject != null)
            fireObject.SetActive(tracked);

        if (challengeManager != null)
        {
            if (tracked)
                challengeManager.SetTrackedFire(fireIndex, fireObject);
            else
                challengeManager.ClearTrackedFire(fireIndex);
        }
    }
}