using System;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 全局游戏状态管理器（单例）
/// 负责：存档点管理、暂停/恢复、场景加载、游戏事件广播
/// </summary>
public class GameManager : MonoBehaviour
{
    // ============================================================
    // 单例
    // ============================================================
    
    private static GameManager _instance;
    public static GameManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<GameManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject(nameof(GameManager));
                    _instance = go.AddComponent<GameManager>();
                    DontDestroyOnLoad(go);
                }
            }
            return _instance;
        }
    }

    /// <summary>
    /// 在场景加载前自动创建 GameManager，避免运行时第一帧卡顿
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void InitializeBeforeSceneLoad()
    {
        // 如果已经有 GameManager 在场景中，跳过
        if (_instance != null) return;

        GameObject go = new GameObject(nameof(GameManager));
        _instance = go.AddComponent<GameManager>();
        DontDestroyOnLoad(go);
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);

        InitializeState();
    }

    // ============================================================
    // 存档点
    // ============================================================

    public Vector2 LastCheckpoint { get; private set; }
    private bool _hasCheckpoint;

    /// <summary>
    /// 设置存档点（由 CheckPoint 脚本调用）
    /// </summary>
    public void SetCheckpoint(Vector2 position)
    {
        LastCheckpoint = position;
        _hasCheckpoint = true;
        OnCheckpointReached?.Invoke(position);
    }

    /// <summary>
    /// 获取复活位置：有存档则返回存档点，否则返回初始位置
    /// </summary>
    public Vector2 GetRespawnPosition()
    {
        return _hasCheckpoint ? LastCheckpoint : _initialSpawnPosition;
    }

    // ============================================================
    // 玩家引用
    // ============================================================

    public GameObject Player { get; private set; }

    /// <summary>
    /// 注册玩家（由 Player 在 Start 时调用）
    /// 如果 ProgressManager 中有选中的检查点，自动传送到那里
    /// </summary>
    public void RegisterPlayer(GameObject player)
    {
        Player = player;

        // 1. 检查 ProgressManager 是否有选中的检查点
        Vector2? checkpointPos = ProgressManager.Instance?.GetSelectedRespawnPosition();
        if (checkpointPos.HasValue)
        {
            player.transform.position = checkpointPos.Value;
            _initialSpawnPosition = checkpointPos.Value;
            _hasCheckpoint = true;
            LastCheckpoint = checkpointPos.Value;
            Debug.Log($"玩家出生在检查点: {checkpointPos.Value}");
            return;
        }

        // 1.5 检查点被选中但没有保存坐标 --> 在场景中找到检查点对象用它的位置
        if (ProgressManager.Instance != null)
        {
            string selectedId = ProgressManager.Instance.Progress.selectedCheckpointId;
            int currentLevel = ProgressManager.Instance.Progress.currentLevel;
            if (!string.IsNullOrEmpty(selectedId))
            {
                // 在所有 CheckPoint 中找 ID 匹配的
                CheckPoint[] allCPs = FindObjectsByType<CheckPoint>(FindObjectsSortMode.None);
                foreach (var cp in allCPs)
                {
                    if (cp.CheckpointId == selectedId && cp.LevelIndex == currentLevel)
                    {
                        Vector2 cpPos = cp.transform.position;
                        player.transform.position = cpPos;
                        _initialSpawnPosition = cpPos;
                        _hasCheckpoint = true;
                        LastCheckpoint = cpPos;
                        // 把坐标存到 ProgressManager 备用
                        ProgressManager.Instance.ActivateCheckpoint(
                            currentLevel, selectedId, cpPos);
                        Debug.Log($"玩家出生在检查点(场景坐标): {cpPos}");
                        return;
                    }
                }
            }
        }

        // 2. 没有选中的检查点，记录初始位置作为备选
        if (!_hasCheckpoint)
        {
            _initialSpawnPosition = player.transform.position;
        }
    }

    private Vector2 _initialSpawnPosition;

    // ============================================================
    // 调试快捷键
    // 暂停键由 PauseUI 独家处理（避免双重触发）
    // ============================================================

    private void Update()
    {
        // 调试快捷键
        if (Input.GetKeyDown(KeyCode.F5))
        {
            if (ProgressManager.Instance != null)
                ProgressManager.Instance.Save();
        }

        if (Input.GetKeyDown(KeyCode.F9))
        {
            if (ProgressManager.Instance != null)
                ProgressManager.Instance.Load();
        }
    }

    /// <summary>
    /// 退出游戏时自动保存进度
    /// </summary>
    private void OnApplicationQuit()
    {
        if (ProgressManager.Instance != null)
            ProgressManager.Instance.Save();
    }

    // ============================================================
    // 暂停管理
    // ============================================================

    public bool IsPaused { get; private set; }

    public void PauseGame()
    {
        if (IsPaused) return;
        IsPaused = true;
        Time.timeScale = 0f;
        OnGamePaused?.Invoke();
    }

    public void ResumeGame()
    {
        if (!IsPaused) return;
        IsPaused = false;
        Time.timeScale = 1f;
        OnGameResumed?.Invoke();
    }

    public void TogglePause()
    {
        if (IsPaused)
            ResumeGame();
        else
            PauseGame();
    }

    // ============================================================
    // 场景管理
    // ============================================================

    /// <summary>
    /// 按场景名称加载
    /// </summary>
    public void LoadScene(string sceneName)
    {
        ResumeGame(); // 切场景前确保时间缩放恢复
        SceneManager.LoadScene(sceneName);
    }

    /// <summary>
    /// 按 Build Index 加载
    /// </summary>
    public void LoadScene(int buildIndex)
    {
        ResumeGame();
        SceneManager.LoadScene(buildIndex);
    }

    // ============================================================
    // 游戏事件（C# 事件，供其他脚本订阅）
    // ============================================================

    // ============================================================
    // 事件发布方法（外部脚本调用这些方法来触发事件）
    // ============================================================

    /// <summary>通知玩家死亡（由 PlayerDead 调用）</summary>
    public void NotifyPlayerDied()
    {
        OnPlayerDied?.Invoke();
    }

    /// <summary>通知食物收集（由 EatFood 调用）</summary>
    public void NotifyFoodCollected(int count)
    {
        OnFoodCollected?.Invoke(count);
    }

    // ============================================================
    // 事件声明（外部脚本可 += 订阅，但只有 GameManager 内部可 Invoke）
    // ============================================================

    /// <summary>玩家到达存档点时触发</summary>
    public event Action<Vector2> OnCheckpointReached;

    /// <summary>玩家死亡时触发</summary>
    public event Action OnPlayerDied;

    /// <summary>收集食物时触发</summary>
    public event Action<int> OnFoodCollected;

    /// <summary>游戏暂停时触发</summary>
    public event Action OnGamePaused;

    /// <summary>游戏恢复时触发</summary>
    public event Action OnGameResumed;

    // ============================================================
    // 初始化
    // ============================================================

    private void InitializeState()
    {
        IsPaused = false;
        _hasCheckpoint = false;
        _initialSpawnPosition = Vector2.zero;

        // 自动附加 PauseUI（如果场景中没有手动放置的话）
        // PauseUI 会接管 Esc 监听和暂停面板控制
        if (GetComponent<PauseUI>() == null)
        {
            gameObject.AddComponent<PauseUI>();
        }
    }

    private void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }
    }
}
