using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 时间减速力场（带持续时间和冷却）
/// Q 键激活，持续一段时间后自动关闭，进入冷却
/// </summary>
public class TimeField : MonoBehaviour
{
    [Header("力场参数")]
    [SerializeField] private float radius = 5f;
    [SerializeField][Range(0.05f, 0.95f)] private float slowFactor = 0.3f;

    /// <summary>供其他脚本读取减速倍率（如移动平台）</summary>
    public float SlowFactor => slowFactor;

    [SerializeField] private LayerMask affectedLayers = -1;

    [Header("持续时间和冷却")]
    [SerializeField] private float duration = 3f;
    [SerializeField] private float cooldown = 5f;

    [Header("操作")]
    [SerializeField] private KeyCode activateKey = KeyCode.Q;

    [Header("调试")]
    [SerializeField] private bool showGizmo = true;

    // ============================================================
    // 运行时状态
    // ============================================================

    private enum FieldState { Ready, Active, Cooldown }
    private FieldState _state = FieldState.Ready;

    private float _durationTimer;
    private float _cooldownTimer;

    // 缓存数组（避免每帧 new 分配）
    private Collider2D[] _overlapResults = new Collider2D[50];

    // 追踪被减速的 Animator
    private HashSet<Animator> _slowedAnimators = new HashSet<Animator>();
    private List<Animator> _currentAnimators = new List<Animator>();

    // ============================================================
    // 状态查询（供外部 UI 或特效使用）
    // ============================================================

    public bool IsActive => _state == FieldState.Active;
    public bool IsOnCooldown => _state == FieldState.Cooldown;
    public float RemainingDuration => _durationTimer;
    public float RemainingCooldown => _cooldownTimer;
    public float DurationNormalized => _durationTimer / duration;
    public float CooldownNormalized => _cooldownTimer / cooldown;

    // ============================================================
    // Unity 生命周期
    // ============================================================

    private void Update()
    {
        switch (_state)
        {
            case FieldState.Ready:
                if (Input.GetKeyDown(activateKey))
                    Activate();
                break;

            case FieldState.Active:
                // 手动关闭
                if (Input.GetKeyDown(activateKey))
                {
                    Deactivate();
                    break;
                }
                // 持续时间倒计时
                _durationTimer -= Time.deltaTime;
                if (_durationTimer <= 0f)
                {
                    _durationTimer = 0f;
                    Deactivate();
                }
                break;

            case FieldState.Cooldown:
                // 冷却倒计时
                _cooldownTimer -= Time.deltaTime;
                if (_cooldownTimer <= 0f)
                {
                    _cooldownTimer = 0f;
                    _state = FieldState.Ready;
                }
                break;
        }
    }

    private void FixedUpdate()
    {
        if (_state != FieldState.Active) return;

        _currentAnimators.Clear();

        // 扫描范围内的碰撞体
        int count = Physics2D.OverlapCircleNonAlloc(
            transform.position, radius, _overlapResults, affectedLayers
        );

        for (int i = 0; i < count; i++)
        {
            Collider2D col = _overlapResults[i];

            // 跳过玩家自己及其子物体
            if (col.gameObject == gameObject ||
                col.transform.IsChildOf(transform) ||
                transform.IsChildOf(col.transform))
                continue;

            // --- 减慢 Rigidbody2D 速度 ---
            Rigidbody2D rb = col.attachedRigidbody;
            if (rb != null && rb.bodyType == RigidbodyType2D.Dynamic && !rb.isKinematic)
            {
                rb.linearVelocity *= slowFactor;
                rb.angularVelocity *= slowFactor;
            }

            // --- 减慢 Animator 播放速度 ---
            Animator anim = col.GetComponentInChildren<Animator>();
            if (anim != null)
            {
                anim.speed = slowFactor;
                _currentAnimators.Add(anim);
            }
        }

        // --- 恢复离开力场的物体的 Animator 速度 ---
        foreach (var anim in _slowedAnimators)
        {
            if (anim != null && !_currentAnimators.Contains(anim))
                anim.speed = 1f;
        }

        // 更新追踪列表
        _slowedAnimators.Clear();
        foreach (var anim in _currentAnimators)
            _slowedAnimators.Add(anim);
    }

    // ============================================================
    // 激活与关闭
    // ============================================================

    private void Activate()
    {
        _state = FieldState.Active;
        _durationTimer = duration;

        // 时间生命系统：使用时间力场消耗1天
        if (TimeLifeManager.Instance != null)
        {
            TimeLifeManager.Instance.ConsumeDay(1);
            Debug.Log($"⏳ 时间力场使用！剩余天数: {TimeLifeManager.Instance.RemainingDays}");
        }

        Debug.Log("⏳ 时间力场: 开启");
    }

    private void Deactivate()
    {
        _state = FieldState.Cooldown;
        _cooldownTimer = cooldown;
        RestoreAllAnimators();
        Debug.Log("⏳ 时间力场: 关闭，进入冷却");
    }

    // ============================================================
    // 恢复所有被减速的 Animator
    // ============================================================

    private void RestoreAllAnimators()
    {
        foreach (var anim in _slowedAnimators)
        {
            if (anim != null)
                anim.speed = 1f;
        }
        _slowedAnimators.Clear();
    }

    // ============================================================
    // 场景辅助
    // ============================================================

    private void OnDrawGizmosSelected()
    {
        if (!showGizmo) return;

        switch (_state)
        {
            case FieldState.Active:
                Gizmos.color = new Color(0.3f, 0.6f, 1f, 0.3f);
                Gizmos.DrawSphere(transform.position, radius);
                break;
            case FieldState.Cooldown:
                Gizmos.color = new Color(0.8f, 0.3f, 0.3f, 0.2f);
                break;
            default:
                Gizmos.color = new Color(0.5f, 0.5f, 0.5f, 0.15f);
                break;
        }
        Gizmos.DrawWireSphere(transform.position, radius);
    }

    private void OnDisable()
    {
        RestoreAllAnimators();
    }
}
