using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("跟随目标")]
    public Transform target;

    [Header("偏移")]
    public float xOffset;
    public float yOffset;

    [Header("平滑")]
    [Range(0f, 1f)]
    public float smoothSpeed = 0.1f;

    private Vector3 _velocity = Vector3.zero;

    private void LateUpdate()
    {
        if (target == null) return;

        Vector3 targetPos = new Vector3(
            target.position.x + xOffset,
            target.position.y + yOffset,
            transform.position.z
        );

        transform.position = Vector3.SmoothDamp(
            transform.position,
            targetPos,
            ref _velocity,
            smoothSpeed
        );
    }
}
