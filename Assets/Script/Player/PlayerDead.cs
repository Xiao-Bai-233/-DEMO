using UnityEngine;

/// <summary>
/// 玩家死亡与复生
/// 复活位置优先级：ProgressManager 选中检查点 > GameManager 存档点 > 初始位置
/// 死亡时调用 TimeLifeManager.ConsumeDay() 消耗天数
/// </summary>
public class PlayerDead : MonoBehaviour
{
    private Rigidbody2D _rb;
    private Animator _anim;
    private Collider2D _collider;
    private bool _isDead;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _anim = GetComponent<Animator>();
        _collider = GetComponent<Collider2D>();
    }

    private void Update()
    {
        // 暴力检测：每帧检查玩家位置是否有陷阱
        if (!_isDead)
            CheckTrapByOverlap();
    }

    // ============================================================
    // 方式一：触发器回调
    // ============================================================

    private void OnTriggerEnter2D(Collider2D collision)
    {
        CheckTrapByCollider(collision);
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        CheckTrapByCollider(collision);
    }

    private void CheckTrapByCollider(Collider2D collision)
    {
        if (_isDead) return;
        if (!collision.CompareTag("tarps")) return;
        Die();
    }

    // ============================================================
    // 方式二：Overlap 暴力检测
    // ============================================================

    private void CheckTrapByOverlap()
    {
        if (_collider == null) return;

        Vector2 center = _collider.bounds.center;
        float radius = _collider.bounds.extents.magnitude * 0.5f;

        Collider2D[] hits = Physics2D.OverlapCircleAll(center, radius);

        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i].CompareTag("tarps"))
            {
                Die();
                return;
            }
        }
    }

    // ============================================================
    // 死亡与复生
    // ============================================================

    private void Die()
    {
        if (_isDead) return;
        _isDead = true;

        _anim.SetTrigger("isDead");
        _rb.bodyType = RigidbodyType2D.Static;

        // 钩锁自动脱离
        GrapplingHook hook = GetComponent<GrapplingHook>();
        if (hook != null) hook.Detach();

        // ============================================================
        // 时间生命系统：消耗一天
        // ============================================================
        if (TimeLifeManager.Instance != null)
        {
            TimeLifeManager.Instance.ConsumeDay(1);

            // 弹出时间条UI（仅死亡时显示）
            if (TimeUI.Instance != null)
                TimeUI.Instance.Show();

            Debug.Log($"💀 死亡！剩余天数: {TimeLifeManager.Instance.RemainingDays}, " +
                      $"当前阶段: {TimeLifeManager.Instance.GetWeekName(TimeLifeManager.Instance.CurrentWeek)}");
        }

        // 广播死亡事件
        if (GameManager.Instance != null)
            GameManager.Instance.NotifyPlayerDied();
    }

    /// <summary>
    /// 动画事件：将玩家传送到复活位置
    /// 优先级：ProgressManager 选中检查点 > GameManager 存档点 > 初始位置
    /// </summary>
    private void Revive_1()
    {
        Vector2 respawnPos;

        // 1. ProgressManager 选中的检查点
        Vector2? progressPos = ProgressManager.Instance?.GetSelectedRespawnPosition();
        if (progressPos.HasValue)
        {
            respawnPos = progressPos.Value;
            Debug.Log($"复活: ProgressManager 检查点位置 {respawnPos}");
        }
        // 2. GameManager 的存档点
        else if (GameManager.Instance != null)
        {
            respawnPos = GameManager.Instance.GetRespawnPosition();
            Debug.Log($"复活: GameManager 存档点位置 {respawnPos}");
        }
        // 3. 原地不动
        else
        {
            respawnPos = transform.position;
            Debug.Log($"复活: 当前位置（无存档）{respawnPos}");
        }

        transform.position = respawnPos;
    }

    /// <summary>
    /// 动画事件：恢复刚体
    /// </summary>
    private void Revive_2()
    {
        _rb.bodyType = RigidbodyType2D.Dynamic;
        _isDead = false;

        // 检查是否游戏结束（天数为0时不再复活）
        if (TimeLifeManager.Instance != null && TimeLifeManager.Instance.RemainingDays <= 0)
        {
            Debug.Log("💀 天数耗尽，无法复活 — 游戏结束");
            _rb.bodyType = RigidbodyType2D.Static;
            // 此处可调用 GameManager.LoadScene("GameOver") 或触发结束事件
        }
    }
}
