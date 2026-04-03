using UnityEngine;

public class FireBurstTowardsCamera : MonoBehaviour
{
    public Transform cameraTarget;
    public float speed = 3f;
    public float stopDistance = 0.5f;

    void Update()
    {
        if (cameraTarget == null) return;

        Vector3 dir = (cameraTarget.position - transform.position);
        float dist = dir.magnitude;

        if (dist > stopDistance)
        {
            transform.position += dir.normalized * speed * Time.deltaTime;
            transform.LookAt(cameraTarget);
        }
    }
}