using UnityEngine;

/// <summary>
/// 钩锁系统
/// 左键发射 → 勾中物体后可通过 W/S 或滚轮收放绳，最大长度封顶
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public class GrapplingHook : MonoBehaviour
{
    [Header("钩锁参数")]
    [SerializeField] private KeyCode fireKey = KeyCode.Mouse0;
    [SerializeField] private float maxRopeLength = 12f;
    [SerializeField] private float minRopeLength = 0.5f;
    [SerializeField] private float ropeAdjustSpeed = 5f;
    [SerializeField] private float pullSpeed = 8f;
    [SerializeField] private LayerMask grappleLayers = -1;

    [Header("视觉")]
    [SerializeField] private LineRenderer ropeRenderer;
    [SerializeField] private Transform aimIndicator; // 可选：瞄准指示器

    // ============================================================
    // 运行时状态
    // ============================================================

    private enum HookState { Idle, Attached }
    private HookState _state = HookState.Idle;

    private DistanceJoint2D _joint;
    private Vector2 _hookPoint;
    private Rigidbody2D _rb;
    private float _currentRopeLength;
    private Camera _mainCamera;

    // 调试钩爪点的可视化
    private Vector2? _lastAimPoint;

    // ============================================================
    // Unity 生命周期
    // ============================================================

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _mainCamera = Camera.main;

        // 创建 DistanceJoint2D（默认禁用）
        _joint = gameObject.AddComponent<DistanceJoint2D>();
        _joint.enabled = false;
        _joint.autoConfigureDistance = false;
        _joint.autoConfigureConnectedAnchor = false;
        _joint.maxDistanceOnly = true;  // 只限制最大距离（不限制最小距离），可自然摆荡

        // 如果没有指定 LineRenderer，自动获取
        if (ropeRenderer == null)
            ropeRenderer = GetComponent<LineRenderer>();
        if (ropeRenderer != null)
            ropeRenderer.enabled = false;
    }

    private void Update()
    {
        switch (_state)
        {
            case HookState.Idle:
                HandleAimInput();
                if (Input.GetKeyDown(fireKey))
                    FireHook();
                break;

            case HookState.Attached:
                HandleRopeAdjust();
                HandleDetachInput();
                break;
        }

        UpdateRopeVisual();
    }

    private void FixedUpdate()
    {
        // 钩索状态时限制最大速度，防止 Joint 拉力导致穿墙
        if (_state == HookState.Attached)
        {
            Vector2 vel = _rb.linearVelocity;
            float maxSpeed = 18f;
            if (vel.magnitude > maxSpeed)
                _rb.linearVelocity = vel.normalized * maxSpeed;
        }
    }

    // ============================================================
    // 瞄准辅助
    // ============================================================

    private void HandleAimInput()
    {
        if (_mainCamera == null) return;

        Vector2 mousePos = _mainCamera.ScreenToWorldPoint(Input.mousePosition);
        _lastAimPoint = mousePos;
    }

    // ============================================================
    // 发射钩锁
    // ============================================================

    private void FireHook()
    {
        if (_mainCamera == null) return;

        // 时间生命系统：使用钩锁消耗1天
        if (TimeLifeManager.Instance != null)
        {
            TimeLifeManager.Instance.ConsumeDay(1);
            Debug.Log($"🪝 钩锁使用！剩余天数: {TimeLifeManager.Instance.RemainingDays}");
        }

        Vector2 mousePos = _mainCamera.ScreenToWorldPoint(Input.mousePosition);
        Vector2 origin = transform.position;
        Vector2 direction = (mousePos - origin).normalized;

        // 从玩家位置向鼠标方向发射射线
        RaycastHit2D hit = Physics2D.Raycast(
            origin, direction, maxRopeLength, grappleLayers
        );

        Debug.DrawRay(origin, direction * maxRopeLength, Color.cyan, 0.5f);

        if (hit.collider == null) return;
        if (hit.collider.gameObject == gameObject) return;
        if (hit.collider.transform.IsChildOf(transform)) return;

        // 命中！建立钩锁
        _hookPoint = hit.point;
        _currentRopeLength = Vector2.Distance(origin, _hookPoint);

        // 配置 DistanceJoint2D
        _joint.connectedAnchor = _hookPoint;
        _joint.connectedBody = hit.rigidbody; // 如果目标有刚体就跟随，没有则固定在世界坐标
        _joint.distance = _currentRopeLength;
        _joint.enabled = true;

        _state = HookState.Attached;

        // 启用绳索渲染
        if (ropeRenderer != null)
        {
            ropeRenderer.positionCount = 2;
            ropeRenderer.enabled = true;
        }

        Debug.Log("钩锁命中: " + hit.collider.name);
    }

    // ============================================================
    // 绳索长度调节
    // ============================================================

    private void HandleRopeAdjust()
    {
        // 滚轮调节
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        // W/S 键调节
        float input = 0f;
        if (Input.GetKey(KeyCode.W)) input += 1f;
        if (Input.GetKey(KeyCode.S)) input -= 1f;

        float totalInput = -scroll * ropeAdjustSpeed + input * Time.deltaTime * ropeAdjustSpeed;

        if (Mathf.Abs(totalInput) > 0.01f)
        {
            _currentRopeLength -= totalInput;
            _currentRopeLength = Mathf.Clamp(_currentRopeLength, minRopeLength, maxRopeLength);
            _joint.distance = _currentRopeLength;
        }
    }

    // ============================================================
    // 脱钩
    // ============================================================

    private void HandleDetachInput()
    {
        // 再次按发射键脱钩
        if (Input.GetKeyDown(fireKey))
        {
            Detach();
            return;
        }

        // 按跳跃键脱钩
        if (Input.GetButtonDown("Jump"))
        {
            Detach();
            // 让跳跃正常执行（由 PlayerMovement 处理）
        }
    }

    public void Detach()
    {
        _joint.enabled = false;
        _state = HookState.Idle;

        if (ropeRenderer != null)
            ropeRenderer.enabled = false;

        Debug.Log("钩锁脱离");
    }

    // ============================================================
    // 绳索渲染
    // ============================================================

    private void UpdateRopeVisual()
    {
        if (ropeRenderer == null || !ropeRenderer.enabled)
            return;

        if (_state == HookState.Attached)
        {
            ropeRenderer.SetPosition(0, transform.position);
            ropeRenderer.SetPosition(1, _hookPoint);
        }
    }

    // ============================================================
    // 调试可视化
    // ============================================================

    private void OnDrawGizmosSelected()
    {
        if (_state == HookState.Attached)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(_hookPoint, 0.2f);
            Gizmos.DrawLine(transform.position, _hookPoint);
        }

        // 最大长度圈
        Gizmos.color = new Color(0.5f, 0.8f, 1f, 0.15f);
        Gizmos.DrawWireSphere(transform.position, maxRopeLength);
    }
}
