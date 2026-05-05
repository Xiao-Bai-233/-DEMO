using UnityEngine;

/// <summary>
/// 玩家动画控制（已迁移到 PlayerMovement，本脚本保留仅用于旧场景兼容）
/// 实际动画由 PlayerMovement.UpdateAnimator() 统一驱动
/// </summary>
[RequireComponent(typeof(Animator))]
public class PlayerAnim : MonoBehaviour
{
    // 所有动画逻辑已迁移至 PlayerMovement，本文件可安全移除
}
