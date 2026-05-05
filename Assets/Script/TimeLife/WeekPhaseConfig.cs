using UnityEngine;

/// <summary>
/// 每"周"的阶段配置（ScriptableObject）
/// 右键 → Create → TimeLife → WeekPhaseConfig 创建实例
/// 一共创建4个：幼儿周、成长周、成熟周、衰退周
/// </summary>
[CreateAssetMenu(fileName = "WeekPhase_", menuName = "TimeLife/WeekPhaseConfig")]
public class WeekPhaseConfig : ScriptableObject
{
    [Header("===== 基本信息 =====")]
    public string phaseName = "未命名阶段";       // 如"幼儿期"
    [TextArea] public string phaseDescription;    // 阶段描述文本

    [Header("===== 角色外观 =====")]
    [Tooltip("该阶段的玩家精灵（单张,非动画用）")]
    public Sprite playerSprite;
    [Tooltip("该阶段的动画控制器（优先生效,有动画用这个）")]
    public RuntimeAnimatorController animatorController;

    [Header("===== 移动参数（该周的基础值）=====")]
    public float maxMoveSpeed = 8f;
    public float jumpHeight = 4f;
    public float groundAcceleration = 60f;
    public float airAcceleration = 30f;
    public float wallSlideSpeed = 2f;

    [Header("===== 技能可用性 =====")]
    public bool jumpEnabled = true;             // 普通跳跃
    public bool wallJumpEnabled = true;        // 墙跳
    public bool grapplingHookEnabled = true;   // 钩锁
    public bool timeFieldEnabled = true;       // 时间力场
}
