using UnityEngine;

[DefaultExecutionOrder(100)]
public sealed class ThirdPersonFollowCamera : MonoBehaviour
{
    public Transform target;
    public float distance = 5.5f;
    public float targetHeight = 1.4f;
    public float pitch = 18f;
    public float yaw;
    public float followSharpness = 12f;
    public LayerMask obstructionMask = ~0;

    private readonly RaycastHit[] hits = new RaycastHit[16];

    private void Start()
    {
        if (target != null) UpdateCamera(true);
    }

    private void LateUpdate()
    {
        if (target == null) return;
        UpdateCamera(false);
    }

    private void UpdateCamera(bool snap)
    {
        Vector3 pivot = target.position + Vector3.up * targetHeight;
        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 direction = rotation * Vector3.back;
        float safeDistance = distance;
        int count = Physics.SphereCastNonAlloc(pivot, 0.2f, direction, hits, distance,
            obstructionMask, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < count; i++)
        {
            if (hits[i].transform == target || hits[i].transform.IsChildOf(target)) continue;
            safeDistance = Mathf.Min(safeDistance, Mathf.Max(0.25f, hits[i].distance - 0.1f));
        }
        Vector3 desired = pivot + direction * safeDistance;
        // Snap inward to keep the camera out of obstructions; smooth ordinary following.
        float blend = snap || safeDistance < distance - 0.01f ? 1f : 1f - Mathf.Exp(-followSharpness * Time.deltaTime);
        transform.position = Vector3.Lerp(transform.position, desired, blend);
        transform.rotation = rotation;
    }
}
