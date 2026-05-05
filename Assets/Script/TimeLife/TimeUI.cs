using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 时间条UI：4行×7列 日历格子
/// 
/// 【行为】
/// - 平时隐藏（脚本保持活跃，持续接收天数事件）
/// - 只有 PlayerDead 调用 Show() 时才弹出，2.5秒后自动隐藏
/// - 技能扣天也在后台更新格子数据，下次死亡时看到的就是最新状态
/// - 格子颜色：消耗=灰色, 当前天=蓝色, 剩余=白色
/// - WeekLabel 显示 "第1周·幼儿期" 等（手动拖入文本组件）
/// - DayCountLabel 显示 "本周剩余 X 天"
/// 
/// 【用户操作】
/// 1. Canvas 下创建空物体 → 挂此脚本
/// 2. 创建 Image 预制体拖入 cellPrefab
/// 3. （可选）拖入 WeekLabel / DayCountLabel 文字组件
/// </summary>
public class TimeUI : MonoBehaviour
{
    // 单例引用，方便 PlayerDead 调用
    public static TimeUI Instance { get; private set; }

    [Header("格子预制体")]
    [SerializeField] private GameObject cellPrefab; // 拖入 Image 预制体

    [Header("网格布局参数")]
    [SerializeField] private int columns = 7;
    [SerializeField] private float cellSize = 30f;
    [SerializeField] private float spacing = 4f;

    [Header("颜色")]
    [SerializeField] private Color activeColor = new Color(0.3f, 0.8f, 1f, 1f);
    [SerializeField] private Color consumedColor = new Color(0.3f, 0.3f, 0.3f, 0.4f);
    [SerializeField] private Color remainingColor = new Color(1f, 1f, 1f, 0.8f);

    [Header("文字标签（可选）")]
    [SerializeField] private Text weekLabel;       // 显示"第1周·幼儿期"
    [SerializeField] private Text dayCountLabel;   // 显示"本周剩余 X 天"

    [Header("弹出行为")]
    [SerializeField] private float displayDuration = 2.5f; // 弹出后多久自动隐藏
    [SerializeField] private GameObject uiContainer;       // 整个UI的根（用于显隐，不设则用自身）

    // 格子数组
    private Image[] _cells;
    private int _totalCells;
    private Coroutine _hideCoroutine;

    // ============================================================
    // Unity 生命周期
    // ============================================================

    private void Awake()
    {
        Instance = this;
        _totalCells = TimeLifeManager.TOTAL_DAYS; // 28
    }

    private void OnEnable()
    {
        if (TimeLifeManager.Instance != null)
        {
            TimeLifeManager.Instance.OnDayChanged += OnDayChanged;
        }
    }

    private void OnDisable()
    {
        if (TimeLifeManager.Instance != null)
        {
            TimeLifeManager.Instance.OnDayChanged -= OnDayChanged;
        }
    }

    private void Start()
    {
        if (cellPrefab == null)
        {
            Debug.LogWarning("TimeUI: cellPrefab 未设置,请拖入一个 Image 预制体");
            return;
        }

        // 先创建网格（只有格子，不再自动创建周标签）
        CreateGrid();

        // 立即初始化格子和标签的颜色/文字（基于当前天数）
        if (TimeLifeManager.Instance != null)
        {
            RefreshCells(TimeLifeManager.Instance.RemainingDays);
            RefreshLabels(TimeLifeManager.Instance.CurrentWeek);
        }

        // 再默认隐藏视觉元素（但脚本本身保持活跃，继续接收事件）
        SetVisible(false);
    }

    // ============================================================
    // 创建网格
    // ============================================================

    private void CreateGrid()
    {
        _cells = new Image[_totalCells];
        RectTransform parentRect = GetComponent<RectTransform>();
        int rows = (_totalCells + columns - 1) / columns;

        for (int i = 0; i < _totalCells; i++)
        {
            int row = i / columns;
            int col = i % columns;

            GameObject cell = Instantiate(cellPrefab, transform);
            cell.name = $"Day_{i + 1}";
            _cells[i] = cell.GetComponent<Image>();

            RectTransform rect = cell.GetComponent<RectTransform>();
            if (rect != null)
            {
                float x = col * (cellSize + spacing);
                float y = -row * (cellSize + spacing);
                rect.anchoredPosition = new Vector2(x, y);
                rect.sizeDelta = new Vector2(cellSize, cellSize);
            }
        }

        // 容器大小
        if (parentRect != null)
        {
            float width = columns * (cellSize + spacing) - spacing;
            float height = rows * (cellSize + spacing) - spacing;
            parentRect.sizeDelta = new Vector2(width, height);
        }
    }


    // ============================================================
    // 天数变化响应
    // ============================================================

    /// <summary>
    /// 消耗天数时触发：刷新格子+标签数据（但不显示UI）
    /// 技能扣天和死亡扣天都会走这里，保持数据一致
    /// </summary>
    private void OnDayChanged(int remainingDays, int currentWeek)
    {
        RefreshCells(remainingDays);
        RefreshLabels(currentWeek);
        // 注意：不在这里 SetVisible(true)！
        // UI 的显示由 PlayerDead 主动调用 Show() 控制
    }

    /// <summary>
    /// 公开方法：弹出时间条UI（仅死亡时调用）
    /// 刷新数据 + 显示 + 自动隐藏
    /// </summary>
    public void Show()
    {
        // 先确保数据是最新的
        if (TimeLifeManager.Instance != null)
        {
            RefreshCells(TimeLifeManager.Instance.RemainingDays);
            RefreshLabels(TimeLifeManager.Instance.CurrentWeek);
        }

        SetVisible(true);

        if (_hideCoroutine != null)
            StopCoroutine(_hideCoroutine);
        _hideCoroutine = StartCoroutine(AutoHideAfterDelay());
    }

    /// <summary>
    /// 刷新28个格子的颜色
    /// </summary>
    private void RefreshCells(int remainingDays)
    {
        if (_cells == null) return;

        int consumedCount = _totalCells - remainingDays;

        for (int i = 0; i < _totalCells; i++)
        {
            if (_cells[i] == null) continue;

            if (i < consumedCount)
            {
                _cells[i].color = consumedColor;      // 已消耗 → 灰色
            }
            else if (i == consumedCount)
            {
                _cells[i].color = activeColor;         // 当前天  → 蓝色
            }
            else
            {
                _cells[i].color = remainingColor;      // 剩余    → 白色
            }
        }
    }

    /// <summary>
    /// 刷新周标签和天数标签
    /// </summary>
    private void RefreshLabels(int currentWeek)
    {
        // WeekLabel: "第1周·幼儿期"（从第1周开始显示）
        if (weekLabel != null)
        {
            int displayWeek = currentWeek + 1; // 0→第1周, 1→第2周...
            string phaseName = TimeLifeManager.Instance.GetWeekName(currentWeek);
            weekLabel.text = $"第{displayWeek}周·{phaseName}";
        }

        // DayCountLabel: 本周剩余天数
        if (dayCountLabel != null)
        {
            int remainThisWeek = TimeLifeManager.Instance.DaysRemainingInWeek;
            dayCountLabel.text = $"本周剩余 {remainThisWeek} 天";
        }
    }

    // ============================================================
    // 显隐控制
    // ============================================================

    /// <summary>
    /// 显示/隐藏视觉元素（绝不 SetActive 脚本自身，否则会丢事件）
    /// - 如果 uiContainer 有设置 → 隐藏它
    /// - 否则 → 隐藏所有子物体（格子 + 标签），脚本保持活跃
    /// </summary>
    private void SetVisible(bool visible)
    {
        if (uiContainer != null)
        {
            uiContainer.SetActive(visible);
            return;
        }

        // 没有 uiContainer：遍历所有子物体，各自切换
        // 脚本本身保持 active, OnDayChanged 事件不会断
        for (int i = 0; i < transform.childCount; i++)
        {
            transform.GetChild(i).gameObject.SetActive(visible);
        }
    }

    private IEnumerator AutoHideAfterDelay()
    {
        yield return new WaitForSecondsRealtime(displayDuration);
        SetVisible(false);
        _hideCoroutine = null;
    }
}
