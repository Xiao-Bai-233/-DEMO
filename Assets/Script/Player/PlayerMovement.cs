using UnityEngine;

/// <summary>
/// 玩家移动控制（核心）
/// 特性：加速/减速移动、变高跳跃、Coyote Time、Jump Buffer、爬墙、墙跳
/// 如果未分配 PlayerData 资产，会使用硬编码默认值，不影响运行
/// </summary>
[RequireComponent(typeof(Rigidbody2D), typeof(Animator))]
[RequireComponent(typeof(Collider2D), typeof(PlayerWallCheck))]
public class PlayerMovement : MonoBehaviour, ISaveable
{
    // ============================================================
    // 配置引用
    // ============================================================

    [Header("配置（可选：不填则用默认值）")]
    [SerializeField] private PlayerData data;
    [SerializeField] private LayerMask groundLayer;

    [Header("===== 时间生命系统 =====")]
    [Tooltip("跳跃是否可用（SkillManager 控制）")]
    [SerializeField] private bool _jumpEnabled = true;
    [Tooltip("墙跳是否可用（SkillManager 控制）")]
    [SerializeField] private bool _wallJumpEnabled = true;

    [Header("音效（留空自动查找）")]
    [SerializeField] private AudioSource jumpAudio;
    [SerializeField] private AudioSource runAudio;

    // ============================================================
    // 组件缓存
    // ============================================================

    private Rigidbody2D _rb;
    private Animator _anim;
    private Collider2D _collider;
    private PlayerWallCheck _wallCheck;

    // ============================================================
    // 运行时状态
    // ============================================================

    // --- 输入 ---
    private float _xInput;
    private bool _jumpPressed;
    private bool _jumpHeld;
    private bool _jumpReleased;

    // --- 地面检测 ---
    private bool _isGrounded;

    // --- 跳跃计时器 ---
    private float _coyoteTimer;
    private float _jumpBufferTimer;

    // --- 墙壁 ---
    private bool _isOnWall;
    private int _wallSide;
    private float _wallJumpLockTimer;

    // --- 朝向 ---
    private bool _isFacingRight = true;

    // --- 当前周移动参数（由 WeekPhaseConfig + 速度曲线共同决定）---
    private float _currentMaxSpeed;
    private float _currentJumpHeight;
    private float _currentWallSlideSpeed;

    // ============================================================
    // Unity 生命周期
    // ============================================================

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _anim = GetComponent<Animator>();
        _collider = GetComponent<Collider2D>();
        _wallCheck = GetComponent<PlayerWallCheck>();

        if (_collider == null)
            Debug.LogWarning("缺少 Collider2D，地面检测可能失效");

        // 物理优化：防止高速碰撞穿透
        _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        // 尝试自动找到 Player 身上的 AudioSource
        AudioSource[] sources = GetComponents<AudioSource>();
        if (jumpAudio == null && sources.Length >= 1)
            jumpAudio = sources[0];
        if (runAudio == null && sources.Length >= 2)
            runAudio = sources[1];
    }

    private void Start()
    {
        // 注册到 GameManager
        if (GameManager.Instance != null)
            GameManager.Instance.RegisterPlayer(gameObject);

        // 默认使用 PlayerData 中的层级
        if (groundLayer == 0 && data != null)
            groundLayer = data.whatIsGround;

        // 初始化移动参数（优先用 WeekPhaseConfig，没有则回退 PlayerData）
        ResetToDefaultParameters();
        if (TimeLifeManager.Instance != null)
            ApplyWeekParameters(TimeLifeManager.Instance.CurrentConfig);
    }

    private void Update()
    {
        ReadInput();
        CheckGround();
        SyncWallState();
        UpdateJumpTimers();
        HandleJumpInput();
        UpdateFacing();
        HandleRunAudio();
        UpdateAnimator();
    }

    private void FixedUpdate()
    {
        HandleHorizontalMovement();
        HandleVerticalMovement();
        HandleWallSlide();

        if (_wallJumpLockTimer > 0f)
            _wallJumpLockTimer -= Time.fixedDeltaTime;
    }

    // ============================================================
    // 输入
    // ============================================================

    private void ReadInput()
    {
        _xInput = Input.GetAxisRaw("Horizontal");
        _jumpPressed = Input.GetButtonDown("Jump");
        _jumpHeld = Input.GetButton("Jump");
        _jumpReleased = Input.GetButtonUp("Jump");
    }

    // ============================================================
    // 地面检测（从脚底发射射线）
    // ============================================================

    private void CheckGround()
    {
        float checkDist = data != null ? data.groundCheckDistance : 0.15f;

        // 从碰撞体底部（脚底）向下打射线
        Vector2 origin = _collider != null
            ? new Vector2(_collider.bounds.center.x, _collider.bounds.min.y)
            : transform.position;

        // 地面层未设置时默认检测所有层（方便新手直接跑起来）
        int mask = groundLayer.value != 0 ? groundLayer : Physics2D.DefaultRaycastLayers;

        RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.down, checkDist, mask);

        _isGrounded = hit.collider != null;
    }

    // ============================================================
    // 墙壁状态同步
    // ============================================================

    private void SyncWallState()
    {
        _isOnWall = _wallCheck.IsOnWall && !_isGrounded;
        _wallSide = _wallCheck.WallSide;
    }

    // ============================================================
    // 跳跃计时器
    // ============================================================

    private void UpdateJumpTimers()
    {
        if (_isGrounded)
        {
            _coyoteTimer = data != null ? data.coyoteTime : 0.12f;
        }
        else
        {
            _coyoteTimer -= Time.deltaTime;
        }

        if (_jumpPressed)
        {
            _jumpBufferTimer = data != null ? data.jumpBufferTime : 0.1f;
        }
        else
        {
            _jumpBufferTimer -= Time.deltaTime;
        }
    }

    // ============================================================
    // 跳跃执行
    // ============================================================

    private void HandleJumpInput()
    {
        // 跳跃被禁用（幼儿期等）
        if (!_jumpEnabled) return;

        // 普通跳跃：Coyote Time + Jump Buffer 配合
        bool canJump = _coyoteTimer > 0f && _jumpBufferTimer > 0f;
        // 墙跳：在墙上 + 按下跳跃 + 墙跳已解锁
        bool canWallJump = _isOnWall && _jumpPressed && _wallJumpLockTimer <= 0f
                           && _wallJumpEnabled;

        if (canJump)
        {
            ExecuteJump();
            _coyoteTimer = 0f;
            _jumpBufferTimer = 0f;
        }
        else if (canWallJump)
        {
            ExecuteWallJump();
            _jumpBufferTimer = 0f;
        }
    }

    private void ExecuteJump()
    {
        // 使用 WeekPhaseConfig 的基础跳跃高度 × 速度曲线倍率
        float jumpHeight = GetEffectiveJumpHeight();

        float jumpVelocity = Mathf.Sqrt(2f * jumpHeight * Mathf.Abs(Physics2D.gravity.y));
        _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, jumpVelocity);

        if (jumpAudio != null)
            jumpAudio.Play();
    }

    private void ExecuteWallJump()
    {
        float horizontalForce = data != null ? data.wallJumpHorizontalForce : 12f;
        float verticalForce = data != null ? data.wallJumpVerticalForce : 10f;
        float lockTime = data != null ? data.wallJumpLockTime : 0.2f;

        float horizontalPush = horizontalForce * -_wallSide;

        _rb.linearVelocity = new Vector2(horizontalPush, verticalForce);
        _wallJumpLockTimer = lockTime;

        // 翻转朝向
        if ((horizontalPush > 0 && !_isFacingRight) || (horizontalPush < 0 && _isFacingRight))
            Flip();

        if (jumpAudio != null)
            jumpAudio.Play();
    }

    // ============================================================
    // 跑步音效
    // ============================================================

    private void HandleRunAudio()
    {
        if (runAudio == null) return;

        bool shouldRun = _isGrounded && Mathf.Abs(_xInput) > 0.01f;

        if (shouldRun && !runAudio.isPlaying)
        {
            runAudio.Play();
        }
        else if (!shouldRun && runAudio.isPlaying)
        {
            runAudio.Stop();
        }
    }

    // ============================================================
    // 水平移动（物理帧）
    // ============================================================

    private void HandleHorizontalMovement()
    {
        if (_wallJumpLockTimer > 0f)
            return;

        // 使用 WeekPhaseConfig 的基础速度 × 速度曲线倍率
        float maxSpeed = GetEffectiveMaxSpeed();
        float targetSpeed = _xInput * maxSpeed;

        // 选择加速度
        float accel = data != null
            ? (_isGrounded ? data.groundAcceleration : data.airAcceleration)
            : 50f;
        float decel = data != null
            ? (_isGrounded ? data.groundDeceleration : data.airDeceleration)
            : 30f;

        float accelRate = Mathf.Abs(targetSpeed) > 0.01f ? accel : decel;

        _rb.linearVelocity = new Vector2(
            Mathf.MoveTowards(_rb.linearVelocity.x, targetSpeed, accelRate * Time.fixedDeltaTime),
            _rb.linearVelocity.y
        );
    }

    // ============================================================
    // 垂直移动（变高跳跃）
    // ============================================================

    private void HandleVerticalMovement()
    {
        Vector2 velocity = _rb.linearVelocity;

        float fallingMult = data != null ? data.fallingGravityMultiplier : 2.5f;
        float lowJumpMult = data != null ? data.lowJumpMultiplier : 2f;

        // 下落时：增加重力倍数，让下落更干脆
        if (velocity.y < 0)
        {
            velocity.y += Physics2D.gravity.y * (fallingMult - 1f) * Time.fixedDeltaTime;
        }
        // 上升时松开跳跃键：增加重力倍数，实现"轻触跳低"
        else if (velocity.y > 0 && !_jumpHeld)
        {
            velocity.y += Physics2D.gravity.y * (lowJumpMult - 1f) * Time.fixedDeltaTime;
        }

        _rb.linearVelocity = velocity;
    }

    // ============================================================
    // 爬墙
    // ============================================================

    private void HandleWallSlide()
    {
        if (!_isOnWall) return;

        float slideSpeed = GetEffectiveWallSlideSpeed();

        if (_rb.linearVelocity.y < -slideSpeed)
        {
            _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, -slideSpeed);
        }
    }

    // ============================================================
    // 朝向
    // ============================================================

    private void UpdateFacing()
    {
        if (_wallJumpLockTimer > 0f)
            return;

        // 趴在墙上时：朝向固定为墙壁方向，不跟随输入
        if (_isOnWall)
        {
            bool shouldFaceRight = _wallSide > 0;
            if (shouldFaceRight != _isFacingRight)
                Flip();
            return;
        }

        // 普通地面/空中：跟随输入方向
        if (_xInput > 0 && !_isFacingRight)
            Flip();
        else if (_xInput < 0 && _isFacingRight)
            Flip();
    }

    private void Flip()
    {
        _isFacingRight = !_isFacingRight;
        Vector3 scale = transform.localScale;
        scale.x *= -1;
        transform.localScale = scale;
    }

    // ============================================================
    // 动画（单一口径）
    // ============================================================

    /// <summary>动画状态枚举（与 Animator Controller 中的 States 对应）</summary>
    public enum AnimState
    {
        Idle = 0,
        Run = 1,
        Jump = 2,
        Fall = 3,
        DoubleJump = 4,
        WallSlide = 5
    }

    /// <summary>当前动画状态（供外部读取，如 PlayerAnim）</summary>
    public AnimState CurrentAnimState { get; private set; } = AnimState.Idle;

    private void UpdateAnimator()
    {
        // --- 计算当前状态 ---
        if (!_isGrounded && _isOnWall)
        {
            CurrentAnimState = AnimState.WallSlide;
        }
        else if (!_isGrounded && _rb.linearVelocity.y > 0.5f)
        {
            CurrentAnimState = AnimState.Jump;
        }
        else if (!_isGrounded && _rb.linearVelocity.y < -0.5f)
        {
            CurrentAnimState = AnimState.Fall;
        }
        else if (_isGrounded && Mathf.Abs(_xInput) > 0.01f)
        {
            CurrentAnimState = AnimState.Run;
        }
        else
        {
            CurrentAnimState = AnimState.Idle;
        }

        // --- 设置 Animator 参数 ---
        _anim.SetInteger("States", (int)CurrentAnimState);
        _anim.SetBool("isGround", _isGrounded);
        _anim.SetBool("inWall", _isOnWall);
        _anim.SetBool("isRun", CurrentAnimState == AnimState.Run);
        _anim.SetBool("isJump", CurrentAnimState == AnimState.Jump);
    }

    // ============================================================
    // 可视化辅助
    // ============================================================

    private void OnDrawGizmos()
    {
        float checkDist = data != null ? data.groundCheckDistance : 0.15f;

        Vector2 origin;
        if (Application.isPlaying && _collider != null)
        {
            origin = new Vector2(_collider.bounds.center.x, _collider.bounds.min.y);
        }
        else if (_collider != null)
        {
            origin = new Vector2(_collider.bounds.center.x, _collider.bounds.min.y);
        }
        else
        {
            origin = transform.position;
        }

        Gizmos.color = Color.green;
        Gizmos.DrawLine(origin, origin + Vector2.down * checkDist);
    }

    // ============================================================
    // 时间生命系统集成（速度曲线 + 换精灵 + 每阶段参数）
    // ============================================================

    /// <summary>
    /// 应用当前周的移动参数（从 WeekPhaseConfig 读取）
    /// </summary>
    private void ApplyWeekParameters(WeekPhaseConfig config)
    {
        if (config == null)
        {
            // 没有配置时回退到 PlayerData 的默认值
            ResetToDefaultParameters();
            return;
        }

        // 基础值来自周配置
        _currentMaxSpeed = config.maxMoveSpeed;
        _currentJumpHeight = config.jumpHeight;
        _currentWallSlideSpeed = config.wallSlideSpeed;

        Debug.Log($"PlayerMovement: 应用阶段参数 (速度={_currentMaxSpeed}, 跳跃={_currentJumpHeight}, 爬墙={_currentWallSlideSpeed})");
    }

    /// <summary>
    /// 回退到 PlayerData 的默认参数
    /// </summary>
    private void ResetToDefaultParameters()
    {
        _currentMaxSpeed = data != null ? data.maxMoveSpeed : 8f;
        _currentJumpHeight = data != null ? data.jumpHeight : 4f;
        _currentWallSlideSpeed = data != null ? data.wallSlideSpeed : 2f;
    }

    /// <summary>获取最终有效速度 = 周基础速度 × 时间曲线倍率</summary>
    private float GetEffectiveMaxSpeed()
    {
        float speed = _currentMaxSpeed;
        if (TimeLifeManager.Instance != null)
            speed *= TimeLifeManager.Instance.GetSpeedMultiplier();
        return speed;
    }

    /// <summary>获取最终有效跳跃高度 = 周基础跳跃 × 时间曲线倍率</summary>
    private float GetEffectiveJumpHeight()
    {
        float height = _currentJumpHeight;
        if (TimeLifeManager.Instance != null)
            height *= TimeLifeManager.Instance.GetSpeedMultiplier();
        return height;
    }

    /// <summary>获取最终有效爬墙速度（爬墙速度不受曲线影响，但每周基础值不同）</summary>
    private float GetEffectiveWallSlideSpeed()
    {
        return _currentWallSlideSpeed;
    }

    private void OnEnable()
    {
        // 订阅周切换事件（用于换精灵 + 换参数）
        if (TimeLifeManager.Instance != null)
        {
            TimeLifeManager.Instance.OnWeekChanged += OnWeekChangedTimeLife;
            // 如果已有 TimeLifeManager，立即应用当前周的配置
            ApplyWeekParameters(TimeLifeManager.Instance.CurrentConfig);
            ApplyWeekSprite(TimeLifeManager.Instance.CurrentWeek);
        }
    }

    private void OnDisable()
    {
        if (TimeLifeManager.Instance != null)
        {
            TimeLifeManager.Instance.OnWeekChanged -= OnWeekChangedTimeLife;
        }
    }

    private void OnWeekChangedTimeLife(int oldWeek, int newWeek)
    {
        ApplyWeekParameters(TimeLifeManager.Instance.CurrentConfig);
        ApplyWeekSprite(newWeek);
        Debug.Log($"PlayerMovement: 切换到 {TimeLifeManager.Instance.GetWeekName(newWeek)}");
    }

    /// <summary>
    /// 应用指定周的玩家外观（精灵或动画控制器）
    /// </summary>
    private void ApplyWeekSprite(int weekIndex)
    {
        WeekPhaseConfig config = TimeLifeManager.Instance.GetWeekConfig(weekIndex);
        if (config == null) return;

        // 方式1：有 AnimatorController → 换动画控制器（含全套动画）
        if (config.animatorController != null && _anim != null)
        {
            _anim.runtimeAnimatorController = config.animatorController;
            return;
        }

        // 方式2：只有单张 Sprite → 直接换 SpriteRenderer
        if (config.playerSprite != null)
        {
            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            if (sr != null)
                sr.sprite = config.playerSprite;
        }
    }

    /// <summary>
    /// 由 SkillManager 调用，控制墙跳是否可用
    /// </summary>
    public void SetWallJumpEnabled(bool enabled)
    {
        _wallJumpEnabled = enabled;
    }

    /// <summary>
    /// 由 SkillManager 调用，控制普通跳跃是否可用
    /// </summary>
    public void SetJumpEnabled(bool enabled)
    {
        _jumpEnabled = enabled;
    }

    // ============================================================
    // ISaveable 存档接口
    // ============================================================

    [System.Serializable]
    private class PlayerMovementSaveData
    {
        public bool isFacingRight;
    }

    public object CaptureState()
    {
        return new PlayerMovementSaveData
        {
            isFacingRight = _isFacingRight
        };
    }

    public void RestoreState(object state)
    {
        if (state is PlayerMovementSaveData saveData)
        {
            _isFacingRight = saveData.isFacingRight;
            if (!_isFacingRight)
            {
                Vector3 scale = transform.localScale;
                scale.x = -Mathf.Abs(scale.x);
                transform.localScale = scale;
            }
        }
    }
}
