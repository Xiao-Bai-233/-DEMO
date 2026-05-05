using UnityEngine;

/// <summary>
/// 玩家墙壁检测（基于射线）
/// 替代旧的触发器式 WallCheck，检测更稳定
/// </summary>
public class PlayerWallCheck : MonoBehaviour
{
    [Header("配置")]
    [SerializeField] private PlayerData playerData;
    [SerializeField] private LayerMask wallLayer;

    /// <summary>当前是否在墙上</summary>
    public bool IsOnWall { get; private set; }

    /// <summary>墙在左侧(-1)还是右侧(1)</summary>
    public int WallSide { get; private set; }

    /// <summary>与墙的距离（归一化 0~1）</summary>
    public float WallDistance { get; private set; }

    // 射线起点偏移
    private Collider2D _collider;
    private float _colliderWidth;

    private void Awake()
    {
        _collider = GetComponent<Collider2D>();
        if (_collider != null)
        {
            _colliderWidth = _collider.bounds.extents.x;
        }
        else
        {
            _colliderWidth = 0.5f; // fallback
        }

        // 默认使用 PlayerData 中的层级，可单独覆写
        if (wallLayer == 0 && playerData != null)
        {
            wallLayer = playerData.whatIsGround;
        }
    }

    private void Update()
    {
        CheckWall();
    }

    private void CheckWall()
    {
        Vector2 origin = transform.position;
        float checkDistance = playerData != null
            ? playerData.wallCheckDistance
            : 0.5f;
        float rayOffset = _colliderWidth + 0.1f;

        // 墙面层未设置时默认检测所有层
        int mask = wallLayer.value != 0 ? wallLayer : Physics2D.DefaultRaycastLayers;

        // --- 检测右侧 ---
        Vector2 rightOrigin = origin + Vector2.right * rayOffset;
        RaycastHit2D rightHit = Physics2D.Raycast(
            rightOrigin, Vector2.right, checkDistance, mask
        );

        // --- 检测左侧 ---
        Vector2 leftOrigin = origin + Vector2.left * rayOffset;
        RaycastHit2D leftHit = Physics2D.Raycast(
            leftOrigin, Vector2.left, checkDistance, mask
        );

        // --- 判定 ---
        if (rightHit.collider != null)
        {
            IsOnWall = true;
            WallSide = 1;
            WallDistance = rightHit.distance / checkDistance;
        }
        else if (leftHit.collider != null)
        {
            IsOnWall = true;
            WallSide = -1;
            WallDistance = leftHit.distance / checkDistance;
        }
        else
        {
            IsOnWall = false;
            WallSide = 0;
            WallDistance = 0f;
        }
    }
}
