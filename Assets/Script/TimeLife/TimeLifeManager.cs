using System;
using UnityEngine;

/// <summary>
/// 时间生命系统核心管理器（单例,自动创建, DontDestroyOnLoad, 自动存档）
/// 
/// 自动从 Resources/TimeLife/ 加载 4 个 WeekPhaseConfig 资产
/// 管理 28天 = 4周 × 7天的生命体系
/// 死亡/技能消耗天数 → 周切换 → 触发外观/能力/移动变化
/// 
/// 【用户配置向导】
/// 参见：Assets/Script/TimeLife/README_Setup.txt
/// </summary>
public class TimeLifeManager : MonoBehaviour
{
    // ============================================================
    // 单例
    // ============================================================

    private static TimeLifeManager _instance;
    public static TimeLifeManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<TimeLifeManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject(nameof(TimeLifeManager));
                    _instance = go.AddComponent<TimeLifeManager>();
                    DontDestroyOnLoad(go);
                }
            }
            return _instance;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void InitializeBeforeSceneLoad()
    {
        if (_instance != null) return;
        GameObject go = new GameObject(nameof(TimeLifeManager));
        _instance = go.AddComponent<TimeLifeManager>();
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
        Initialize();
    }

    private void OnDestroy()
    {
        if (_instance == this)
        {
            SaveTimeData(); // 退出时保存天数
            _instance = null;
        }
    }

    // ============================================================
    // 常量
    // ============================================================

    public const int DAYS_PER_WEEK = 7;
    public const int TOTAL_WEEKS = 4;
    public const int TOTAL_DAYS = DAYS_PER_WEEK * TOTAL_WEEKS; // 28

    // ============================================================
    // 阶段配置（Inspector 和 Resources 双通道加载）
    // ============================================================

    [Header("===== 四阶段配置（可选：在 Inspector 手动拖入）=====")]
    public WeekPhaseConfig[] weekPhases = new WeekPhaseConfig[TOTAL_WEEKS];

    [Header("===== 速度平滑曲线 =====")]
    [Tooltip("X轴: 剩余天数归一化(0~1), Y轴: 速度倍率\n建议: 幼儿低→成长升→成熟最高→衰退降")]
    public AnimationCurve speedCurve = AnimationCurve.Linear(0f, 0.4f, 1f, 1f);

    // ============================================================
    // 运行时状态
    // ============================================================

    /// <summary>当前剩余天数（0=游戏结束）</summary>
    public int RemainingDays { get; private set; }

    /// <summary>当前周索引（0~3）</summary>
    public int CurrentWeek { get; private set; }

    /// <summary>当前周的第几天（1~7）</summary>
    public int DayInWeek { get; private set; }

    /// <summary>当前周的配置</summary>
    public WeekPhaseConfig CurrentConfig => GetWeekConfig(CurrentWeek);

    /// <summary>本周还剩多少天（1~7,用于 UI 显示）</summary>
    public int DaysRemainingInWeek => DAYS_PER_WEEK - (TOTAL_DAYS - RemainingDays) % DAYS_PER_WEEK;

    // ============================================================
    // 事件
    // ============================================================

    /// <summary>天数变化时：参数(int remainingDays, int currentWeek)</summary>
    public event Action<int, int> OnDayChanged;

    /// <summary>周切换时：参数(int oldWeek, int newWeek)</summary>
    public event Action<int, int> OnWeekChanged;

    /// <summary>游戏结束时触发</summary>
    public event Action OnGameOver;

    // ============================================================
    // 独立文件持久化（不依赖 SaveManager 的 ISaveable 存档流程）
    // ============================================================

    private const string TIME_DATA_FILE = "time_data.json";

    private string TimeDataFilePath =>
        System.IO.Path.Combine(Application.persistentDataPath, TIME_DATA_FILE);

    [Serializable]
    private class TimePersistData
    {
        public int remainingDays;
    }

    /// <summary>
    /// 保存天数到独立文件（游戏退出时自动调用）
    /// </summary>
    public void SaveTimeData()
    {
        TimePersistData data = new TimePersistData { remainingDays = RemainingDays };
        string json = JsonUtility.ToJson(data);
        System.IO.File.WriteAllText(TimeDataFilePath, json);
        Debug.Log($"TimeLifeManager: 保存天数到 {TimeDataFilePath} ({RemainingDays}天)");
    }

    /// <summary>
    /// 从独立文件读取天数（游戏启动时自动调用）
    /// 返回 true 表示读到有效数据
    /// </summary>
    public bool LoadTimeData()
    {
        if (!System.IO.File.Exists(TimeDataFilePath))
            return false;

        string json = System.IO.File.ReadAllText(TimeDataFilePath);
        TimePersistData data = JsonUtility.FromJson<TimePersistData>(json);
        if (data == null || data.remainingDays <= 0)
            return false;

        int prevWeek = CurrentWeek;
        RemainingDays = Mathf.Clamp(data.remainingDays, 1, TOTAL_DAYS);

        int consumedDays = TOTAL_DAYS - RemainingDays;
        CurrentWeek = Mathf.Min(consumedDays / DAYS_PER_WEEK, TOTAL_WEEKS - 1);
        DayInWeek = (consumedDays % DAYS_PER_WEEK) + 1;

        OnDayChanged?.Invoke(RemainingDays, CurrentWeek);
        if (CurrentWeek != prevWeek)
            OnWeekChanged?.Invoke(prevWeek, CurrentWeek);

        Debug.Log($"TimeLifeManager: 加载天数 {RemainingDays} (第{CurrentWeek + 1}周)");
        return true;
    }

    /// <summary>
    /// 删除时间数据文件（用于重置）
    /// </summary>
    public void DeleteTimeDataFile()
    {
        if (System.IO.File.Exists(TimeDataFilePath))
        {
            System.IO.File.Delete(TimeDataFilePath);
            Debug.Log("TimeLifeManager: 已删除时间数据文件");
        }
    }

    // ============================================================
    // 开发期专用：重置天数
    // ============================================================

    private void Update()
    {
        // 仅在编辑器或开发构建中生效
        // Ctrl+Shift+R 重置天数到28
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (Input.GetKey(KeyCode.LeftControl) && Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.R))
        {
            DevResetDays();
        }
#endif
    }

    /// <summary>
    /// 重置天数到满值28，同时重置进度数据
    /// 【仅在编辑器或Development Build中可用】
    /// </summary>
    public void DevResetDays()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        int prevWeek = CurrentWeek;

        RemainingDays = TOTAL_DAYS;
        CurrentWeek = 0;
        DayInWeek = 1;

        OnDayChanged?.Invoke(RemainingDays, CurrentWeek);
        if (CurrentWeek != prevWeek)
            OnWeekChanged?.Invoke(prevWeek, CurrentWeek);

        // 删除持久化文件
        DeleteTimeDataFile();

        // 同时重置关卡进度
        if (ProgressManager.Instance != null)
            ProgressManager.Instance.ResetAllProgress();

        // 删除存档文件
        if (SaveManager.Instance != null)
            SaveManager.Instance.DeleteAllSlots();

        Debug.Log("[DEV] 🗑️ 天数已重置为28天，进度已清空");
#else
        Debug.LogWarning("DevResetDays 仅在编辑器或 Development Build 中可用");
#endif
    }

    // ============================================================
    // 自动保存（退出游戏时）
    // OnDestroy 中的保存已在单例清理中处理
    // ============================================================

    private void OnApplicationQuit()
    {
        SaveTimeData();
    }

    // ============================================================
    // 初始化
    // ============================================================

    private void Initialize()
    {
        RemainingDays = TOTAL_DAYS;
        CurrentWeek = 0;
        DayInWeek = 1;

        // 启动时加载之前保存的天数（如果文件存在）
        LoadTimeData();

        // 加载阶段配置（Inspector > Resources 自动加载 > 兜底默认值）
        LoadPhaseConfigs();

        // 自动附加 SkillManager
        if (GetComponent<SkillManager>() == null)
            gameObject.AddComponent<SkillManager>();
    }

    private void LoadPhaseConfigs()
    {
        bool anyMissing = false;
        for (int i = 0; i < TOTAL_WEEKS; i++)
        {
            // 如果 Inspector 没拖入，从 Resources 自动加载
            if (weekPhases[i] == null)
            {
                weekPhases[i] = Resources.Load<WeekPhaseConfig>($"TimeLife/WeekPhase_{i}");
                if (weekPhases[i] != null)
                    Debug.Log($"TimeLifeManager: 自动加载 Resources/TimeLife/WeekPhase_{i}");
            }

            if (weekPhases[i] == null)
            {
                anyMissing = true;
            }
        }

        if (anyMissing)
        {
            Debug.LogWarning("TimeLifeManager: 部分阶段配置未找到。请创建 WeekPhaseConfig 资产:\n" +
                             "1. 在 Project 窗口右键 → Create → TimeLife → WeekPhaseConfig\n" +
                             "2. 命名为 WeekPhase_0 ~ WeekPhase_3\n" +
                             "3. 放到 Assets/Resources/TimeLife/ 文件夹下");
        }
    }

    // ============================================================
    // 公共方法
    // ============================================================

    /// <summary>
    /// 消耗天数（死亡或技能使用）
    /// </summary>
    /// <param name="amount">消耗的天数,默认1</param>
    public void ConsumeDay(int amount = 1)
    {
        if (RemainingDays <= 0) return;

        int prevDays = RemainingDays;
        int prevWeek = CurrentWeek;

        RemainingDays = Mathf.Max(0, RemainingDays - amount);

        // ---- 修复：周数由"已消耗天数"决定 ----
        // 剩余28天 = 已消耗0天 = 第0周(幼儿期)
        // 剩余21天 = 已消耗7天 = 第1周(成长期)
        // 剩余14天 = 已消耗14天 = 第2周(成熟期)
        // 剩余7天  = 已消耗21天 = 第3周(衰退期)
        int consumedDays = TOTAL_DAYS - RemainingDays;
        int newWeek = consumedDays / DAYS_PER_WEEK;
        if (newWeek >= TOTAL_WEEKS) newWeek = TOTAL_WEEKS - 1; // 兜底

        // 当前周的"第几天"(1~7)：已消耗天数在周内的位置 +1
        int newDayInWeek = (consumedDays % DAYS_PER_WEEK) + 1;

        CurrentWeek = newWeek;
        DayInWeek = newDayInWeek;

        // 触发事件
        OnDayChanged?.Invoke(RemainingDays, CurrentWeek);

        // 跨周了
        if (CurrentWeek != prevWeek)
        {
            OnWeekChanged?.Invoke(prevWeek, CurrentWeek);
            Debug.Log($"⏰ 进入新阶段: {GetWeekName(CurrentWeek)} (第{CurrentWeek + 1}周)");
        }

        // 游戏结束
        if (RemainingDays <= 0)
        {
            OnGameOver?.Invoke();
            Debug.Log("💀 时间耗尽,游戏结束");
        }
    }

    /// <summary>
    /// 获取当前速度倍率（由 AnimationCurve 计算）
    /// 归一化天数 = RemainingDays / TOTAL_DAYS → 查曲线
    /// </summary>
    public float GetSpeedMultiplier()
    {
        float t = (float)RemainingDays / TOTAL_DAYS;
        return speedCurve.Evaluate(t);
    }

    /// <summary>
    /// 获取指定周的配置
    /// </summary>
    public WeekPhaseConfig GetWeekConfig(int weekIndex)
    {
        if (weekPhases == null || weekIndex < 0 || weekIndex >= weekPhases.Length)
            return null;
        return weekPhases[weekIndex];
    }

    /// <summary>
    /// 获取周名称（带配置则用配置名,否则用默认名）
    /// </summary>
    public string GetWeekName(int weekIndex)
    {
        var cfg = GetWeekConfig(weekIndex);
        return cfg != null ? cfg.phaseName : $"第{weekIndex + 1}周";
    }
}

// ============================================================
// 编辑器菜单：不运行游戏也能重置（Tools → Reset Time Data）
// ============================================================

#if UNITY_EDITOR
public static class TimeLifeEditorTools
{
    [UnityEditor.MenuItem("Tools/Reset Time Data")]
    private static void ResetTimeData()
    {
        string timePath = System.IO.Path.Combine(
            UnityEngine.Application.persistentDataPath, "time_data.json");
        string progressPath = System.IO.Path.Combine(
            UnityEngine.Application.persistentDataPath, "progress.json");

        bool deletedAny = false;

        if (System.IO.File.Exists(timePath))
        {
            System.IO.File.Delete(timePath);
            UnityEngine.Debug.Log($"[Editor] 已删除 {timePath}");
            deletedAny = true;
        }

        if (System.IO.File.Exists(progressPath))
        {
            System.IO.File.Delete(progressPath);
            UnityEngine.Debug.Log($"[Editor] 已删除 {progressPath}");
            deletedAny = true;
        }

        // 也删除 SaveManager 的存档
        string saveDir = UnityEngine.Application.persistentDataPath;
        var saveFiles = System.IO.Directory.GetFiles(saveDir, "save_*.json");
        foreach (var f in saveFiles)
        {
            System.IO.File.Delete(f);
            UnityEngine.Debug.Log($"[Editor] 已删除 {f}");
            deletedAny = true;
        }

        if (deletedAny)
            UnityEngine.Debug.Log("[Editor] ✅ 所有持久化数据已清空。启动游戏后将回到初始状态。");
        else
            UnityEngine.Debug.Log("[Editor] ℹ️ 没有找到需要删除的存档文件。");
    }
}
#endif
