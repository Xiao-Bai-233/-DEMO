using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 移动平台：在 PosA 和 PosB 之间往返
/// Kinematic 模式 + 主动推动站着的角色
/// </summary>
[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class PlatfoemMove : MonoBehaviour
{
    [Header("移动目标点")]
    public Transform PosA;
    public Transform PosB;

    [Header("移动参数")]
    [SerializeField] private float moveSpeed = 2f;

    [Header("时间减速")]
    [SerializeField] private bool isSlowedByTimeField = true;

    private Transform _target;
    private Rigidbody2D _rb;
    private Vector2 _previousPosition;
    private List<Rigidbody2D> _riders = new List<Rigidbody2D>();

    private void Start()
    {
        _rb = GetComponent<Rigidbody2D>();
        _rb.bodyType = RigidbodyType2D.Kinematic;
        _target = PosA;
        _previousPosition = _rb.position;
    }

    private void FixedUpdate()
    {
        if (PosA == null || PosB == null) return;

        // 计算目标位置
        float distance = Vector2.Distance(_rb.position, _target.position);
        if (distance < 0.1f)
            _target = _target == PosA ? PosB : PosA;

        // 速度（含时间减速）
        float speed = moveSpeed;
        if (isSlowedByTimeField)
        {
            TimeField tf = FindFirstObjectByType<TimeField>();
            if (tf != null && tf.IsActive) speed *= tf.SlowFactor;
        }

        // 移动平台
        Vector2 newPos = Vector2.MoveTowards(_rb.position, _target.position, speed * Time.fixedDeltaTime);
        Vector2 movementDelta = newPos - _previousPosition;

        _rb.MovePosition(newPos);

        // 推动平台上站着的角色（不用 SetParent，不会冲突）
        for (int i = _riders.Count - 1; i >= 0; i--)
        {
            if (_riders[i] != null)
                _riders[i].transform.position += (Vector3)movementDelta;
            else
                _riders.RemoveAt(i);
        }

        _previousPosition = newPos;
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (!collision.collider.CompareTag("Player")) return;
        Rigidbody2D riderRb = collision.rigidbody;
        if (riderRb != null && !_riders.Contains(riderRb))
            _riders.Add(riderRb);
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        Rigidbody2D riderRb = collision.rigidbody;
        if (riderRb != null)
            _riders.Remove(riderRb);
    }

    private void OnDrawGizmosSelected()
    {
        if (PosA != null) { Gizmos.color = Color.cyan; Gizmos.DrawWireSphere(PosA.position, 0.3f); }
        if (PosB != null) { Gizmos.color = Color.magenta; Gizmos.DrawWireSphere(PosB.position, 0.3f); }
        if (PosA != null && PosB != null) { Gizmos.color = Color.yellow; Gizmos.DrawLine(PosA.position, PosB.position); }
    }
}
