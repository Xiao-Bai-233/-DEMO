using UnityEngine;

/// <summary>
/// 检查点（关卡制版本）
/// 每个检查点有唯一 ID 和所属关卡编号
/// 激活后记录到 ProgressManager，可被选择为重生点
/// </summary>
public class CheckPoint : MonoBehaviour
{
    [Header("检查点参数")]
    [SerializeField] private string checkpointId = "1-1";  // 格式："{关卡编号}-{序号}"
    [SerializeField] private int levelIndex = 1;            // 所属关卡

    [Header("视觉")]
    [SerializeField] private GameObject activatedVisual;   // 激活状态的外观（可选）
    [SerializeField] private GameObject inactiveVisual;    // 未激活状态的外观（可选）

    private Animator _anim;
    private bool _isActivated;

    public string CheckpointId => checkpointId;
    public int LevelIndex => levelIndex;

    private void Start()
    {
        // 注意：用 GetComponentInChildren，Animator 可能在子物体上（比如旗帜）
        _anim = GetComponentInChildren<Animator>();

        // 从 ProgressManager 读取是否已激活
        if (ProgressManager.Instance != null &&
            ProgressManager.Instance.IsCheckpointActivated(levelIndex, checkpointId))
        {
            _isActivated = true;
            UpdateVisual(true);
        }
        else
        {
            UpdateVisual(false);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player")) return;

        // 无论是否已激活，都记录位置到 GameManager（确保复活点正确）
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetCheckpoint(transform.position);
        }

        // 如果已激活（比如自动激活的第一个检查点），补充记录位置后播动画
        if (_isActivated)
        {
            // 更新 ProgressManager 中的位置信息
            if (ProgressManager.Instance != null)
            {
                ProgressManager.Instance.ActivateCheckpoint(levelIndex, checkpointId, transform.position);
            }
            PlayActivateAnim();
            return;
        }

        Activate();
    }

    /// <summary>激活检查点</summary>
    private void Activate()
    {
        _isActivated = true;

        // 通知 ProgressManager（记录位置和激活状态）
        if (ProgressManager.Instance != null)
        {
            ProgressManager.Instance.ActivateCheckpoint(levelIndex, checkpointId, transform.position);
        }

        // 通知 GameManager（兼容旧存档点逻辑）
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetCheckpoint(transform.position);
        }

        // 激活动画 + 视觉切换
        PlayActivateAnim();
        UpdateVisual(true);
    }

    /// <summary>播放激活动画（不管是否已激活都会播）</summary>
    private void PlayActivateAnim()
    {
        if (_anim != null)
        {
            _anim.SetTrigger("isTouch");
        }
    }

    /// <summary>更新视觉状态</summary>
    private void UpdateVisual(bool activated)
    {
        if (activatedVisual != null)
            activatedVisual.SetActive(activated);
        if (inactiveVisual != null)
            inactiveVisual.SetActive(!activated);
    }

    private void OnDrawGizmos()
    {
        // 在 Scene 视图中标出检查点 ID
        Gizmos.color = _isActivated ? Color.green : Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 0.3f);

#if UNITY_EDITOR
        UnityEditor.Handles.Label(
            transform.position + Vector3.up * 0.5f,
            $"L{levelIndex} {checkpointId}"
        );
#endif
    }
}
