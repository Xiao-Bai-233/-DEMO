using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 技能管理器：根据当前周阶段启用/禁用技能
/// 自动附加到 TimeLifeManager 上
/// 
/// 技能开关逻辑：
/// - GrapplingHook: 启用/禁用整个组件
/// - TimeField:        启用/禁用整个组件
/// - WallJump:         通过 PlayerMovement 的开关控制
/// </summary>
public class SkillManager : MonoBehaviour
{
    // 组件缓存
    private GrapplingHook _hook;
    private TimeField _timeField;
    private PlayerMovement _playerMovement;

    // 是否已成功找到玩家
    private bool _playerFound;

    private void Awake()
    {
        // 启动延迟查找（玩家可能在场景加载后才创建）
        StartCoroutine(DelayedFindPlayer());
    }

    private IEnumerator DelayedFindPlayer()
    {
        // 最多重试 30 帧（约0.5秒），直到找到玩家
        for (int i = 0; i < 30; i++)
        {
            FindPlayerComponents();
            if (_playerFound)
            {
                ApplyCurrentWeekSkills();
                yield break;
            }
            yield return null; // 等一帧
        }

        // 30帧后还没找到，走场景加载监听兜底
        Debug.LogWarning("SkillManager: 30帧内未找到玩家，等待场景加载事件...");
    }

    private void OnEnable()
    {
        if (TimeLifeManager.Instance != null)
        {
            TimeLifeManager.Instance.OnWeekChanged += OnWeekChanged;
            TimeLifeManager.Instance.OnDayChanged += OnDayChanged;
        }

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        if (TimeLifeManager.Instance != null)
        {
            TimeLifeManager.Instance.OnWeekChanged -= OnWeekChanged;
            TimeLifeManager.Instance.OnDayChanged -= OnDayChanged;
        }

        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private Coroutine _loadRetryCoroutine;

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 切场景后清除缓存，重新查找
        _hook = null;
        _timeField = null;
        _playerMovement = null;
        _playerFound = false;

        // 延迟多帧重试，等场景中的 Player Start() 执行完
        if (_loadRetryCoroutine != null)
            StopCoroutine(_loadRetryCoroutine);
        _loadRetryCoroutine = StartCoroutine(DelayedSceneLoadRetry());
    }

    private System.Collections.IEnumerator DelayedSceneLoadRetry()
    {
        // 等 5 帧，确保 PlayerMovement.Start() 已执行
        for (int i = 0; i < 5; i++)
            yield return null;

        FindPlayerComponents();
        ApplyCurrentWeekSkills();

        // 如果还没找到，继续重试
        if (!_playerFound)
        {
            for (int i = 0; i < 25; i++)
            {
                yield return null;
                FindPlayerComponents();
                if (_playerFound)
                {
                    ApplyCurrentWeekSkills();
                    Debug.Log("SkillManager: 延迟查找成功");
                    yield break;
                }
            }
            Debug.LogWarning("SkillManager: 场景加载后30帧仍未找到玩家");
        }
        else
        {
            Debug.Log($"SkillManager: 场景加载后应用技能状态 " +
                      $"_hook={_hook?.enabled} _timeField={_timeField?.enabled} " +
                      $"_wallJump={_playerMovement != null}");
        }
    }

    private void OnWeekChanged(int oldWeek, int newWeek)
    {
        FindPlayerComponents();
        ApplyCurrentWeekSkills();
    }

    private void OnDayChanged(int remainingDays, int currentWeek)
    {
        // 如果还没找到玩家，每次天数变化时再试一次
        if (!_playerFound)
        {
            FindPlayerComponents();
            if (_playerFound)
                ApplyCurrentWeekSkills();
        }
    }

    // ============================================================
    // 查找玩家（可重复调用，安全）
    // ============================================================

    private void FindPlayerComponents()
    {
        GameObject player = GameManager.Instance != null
            ? GameManager.Instance.Player
            : GameObject.FindGameObjectWithTag("Player");

        if (player == null)
        {
            _playerFound = false;
            return;
        }

        // 只在组件未缓存时查找（避免每帧 GetComponent）
        if (_hook == null)
            _hook = player.GetComponent<GrapplingHook>();
        if (_timeField == null)
            _timeField = player.GetComponent<TimeField>();
        if (_playerMovement == null)
            _playerMovement = player.GetComponent<PlayerMovement>();

        // 三个组件只要有任意一个找到了就算数
        _playerFound = _hook != null || _timeField != null || _playerMovement != null;
    }

    // ============================================================
    // 应用技能开关
    // ============================================================

    public void ApplyCurrentWeekSkills()
    {
        var config = TimeLifeManager.Instance.CurrentConfig;
        if (config == null)
        {
            Debug.LogWarning($"SkillManager: 当前周({TimeLifeManager.Instance.CurrentWeek}) 配置为空，" +
                             "请确认 Resources/TimeLife/ 下有 WeekPhase_0~3");
            return;
        }

        // 钩锁（禁用前先脱离）
        if (_hook != null)
        {
            if (!config.grapplingHookEnabled && _hook.enabled)
                _hook.Detach();
            _hook.enabled = config.grapplingHookEnabled;
        }

        // 时间力场
        if (_timeField != null)
        {
            _timeField.enabled = config.timeFieldEnabled;
        }

        // 跳跃
        if (_playerMovement != null)
        {
            _playerMovement.SetJumpEnabled(config.jumpEnabled);
            _playerMovement.SetWallJumpEnabled(config.wallJumpEnabled);
        }

        Debug.Log($"SkillManager: 技能状态更新 ({TimeLifeManager.Instance.GetWeekName(TimeLifeManager.Instance.CurrentWeek)})" +
                  $" 跳跃={config.jumpEnabled} 墙跳={config.wallJumpEnabled} 钩锁={config.grapplingHookEnabled} 时间力场={config.timeFieldEnabled}");
    }
}
