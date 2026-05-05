using UnityEngine;

/// <summary>
/// 玩家移动与跳跃参数配置（ScriptableObject）
/// 可在 Assets → Create → PlayerData 菜单中创建实例
/// </summary>
[CreateAssetMenu(fileName = "PlayerData", menuName = "Game/PlayerData")]
public class PlayerData : ScriptableObject
{
    // ============================================================
    // 水平移动
    // ============================================================

    [Header("===== 水平移动 =====")]

    [Tooltip("最大移动速度")]
    public float maxMoveSpeed = 8f;

    [Tooltip("地面加速度")]
    public float groundAcceleration = 60f;

    [Tooltip("地面减速度（松手后减速）")]
    public float groundDeceleration = 40f;

    [Tooltip("空中加速度")]
    public float airAcceleration = 30f;

    [Tooltip("空中减速度")]
    public float airDeceleration = 20f;

    // ============================================================
    // 跳跃
    // ============================================================

    [Header("===== 跳跃 =====")]

    [Tooltip("跳跃高度")]
    public float jumpHeight = 4f;

    [Tooltip("到达跳跃顶点的时间（秒），决定了跳跃弧线的缓急")]
    public float timeToJumpApex = 0.4f;

    [Tooltip("下落时的重力倍数（>1 下落更干脆）")]
    public float fallingGravityMultiplier = 2.5f;

    [Tooltip("轻点跳跃时的重力倍数（>1 跳得越低）")]
    public float lowJumpMultiplier = 2f;

    [Tooltip("离开地面后还能跳跃的缓冲时间（秒）")]
    public float coyoteTime = 0.12f;

    [Tooltip("落地前按下跳跃键的缓冲时间（秒）")]
    public float jumpBufferTime = 0.1f;

    // ============================================================
    // 墙壁相关
    // ============================================================

    [Header("===== 墙壁 =====")]

    [Tooltip("爬墙时的最大下落速度")]
    public float wallSlideSpeed = 2f;

    [Tooltip("墙跳的水平推力")]
    public float wallJumpHorizontalForce = 12f;

    [Tooltip("墙跳的垂直推力")]
    public float wallJumpVerticalForce = 10f;

    [Tooltip("墙跳后不能重新吸墙的时间（秒）")]
    public float wallJumpLockTime = 0.2f;

    [Tooltip("墙壁检测射线的长度")]
    public float wallCheckDistance = 0.5f;

    // ============================================================
    // 地面检测
    // ============================================================

    [Header("===== 地面检测 =====")]

    [Tooltip("地面检测射线的长度")]
    public float groundCheckDistance = 0.15f;

    [Tooltip("哪些层级算地面")]
    public LayerMask whatIsGround;
}
