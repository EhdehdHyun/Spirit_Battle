using UnityEngine;

/// <summary>
/// Dynamic Rigidbody 기반 캐릭터 컨트롤러
/// - Transform.position을 직접 수정하지 않고 Rigidbody.velocity로만 이동
/// - 이동, 점프, 대쉬, 넉백, 낙하 상태 관리 포함
/// </summary>
[RequireComponent(typeof(Rigidbody), typeof(Collider))]
public class PhysicsCharacter : MonoBehaviour
{
    [Header("이동 설정")]
    [Tooltip("목표 수평 이동 속도")]
    public float moveSpeed = 8f;

    [Tooltip("목표 속도까지 가속하는 정도")]
    public float acceleration = 40f;

    [Tooltip("입력이 없을 때 0으로 감속하는 정도")]
    public float deceleration = 30f;

    [Header("점프 설정")]
    [Tooltip("점프 시 위로 부여할 초기 속도")]
    public float jumpPower = 10f;

    [Tooltip("코요테 타임. 지면에서 떨어진 후 이 시간 동안은 점프 허용")]
    public float coyoteTime = 0.1f;

    [Header("중력 설정")]
    [Tooltip("초당 y 속도에 더해질 중력 값(음수)")]
    public float gravity = -25f;

    [Tooltip("최대 낙하 속도(음수)")]
    public float terminalVelocity = -40f;

    [Header("수평 감속")]
    [Tooltip("지면에서 입력이 없을 때 수평 감속")]
    public float groundFriction = 2f;

    [Tooltip("공중에서 수평 감속")]
    public float airHorizontalDrag = 1f;

    [Header("대쉬")]
    [Tooltip("대쉬 중 수평 속도")]
    public float dashSpeed = 18f;

    [Tooltip("대쉬 지속 시간")]
    public float dashDuration = 0.2f;

    [Tooltip("대쉬 쿨타임")]
    public float dashCooldown = 0.5f;

    [Header("넉백")]
    [Tooltip("넉백 수평 속도가 줄어드는 정도")]
    public float knockbackDrag = 10f;

    [Header("지면 체크")]
    [Tooltip("발밑 지면 체크 거리")]
    public float groundCheckDistance = 0.3f;

    [Tooltip("지면/벽으로 인식할 레이어 마스크")]
    public LayerMask groundMask;

    [Header("이동 정지")]
    public bool movementLock = false;

    // 외부 조회용 프로퍼티
    public Vector3 Velocity => _rb.velocity;
    public bool IsGrounded => _isGrounded;
    public bool IsFalling { get; private set; }

    // 내부 필드
    Rigidbody _rb;

    // 입력 벡터 (XZ)
    Vector2 _moveInput;

    // 지면 상태
    bool _isGrounded;
    bool _wasGrounded;
    float _lastGroundedTime;

    // 대쉬 상태
    bool _isDashing;
    float _dashTimer;
    float _dashCooldownTimer;
    Vector3 _dashDirection;

    // 점프 입력 버퍼
    bool _jumpRequested;

    // 외부 수평 임펄스 (넉백 등)
    Vector3 _externalHorizontalVelocity;

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();

        // Dynamic 설정
        _rb.isKinematic = false;
        _rb.useGravity = false; // 중력은 직접 처리
        _rb.constraints = RigidbodyConstraints.FreezeRotationX
                        | RigidbodyConstraints.FreezeRotationY
                        | RigidbodyConstraints.FreezeRotationZ;
        _rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    // 입력은 보통 외부 컨트롤러에서 넘겨받지만,
    // 단독 테스트용으로 여기서 직접 처리할 수도 있다.
    // 실제 프로젝트에서는 PlayerInputController에서 SetMoveInput/RequestJump/TryDash 호출.
    //void Update()
    //{
    //    // 필요 없으면 이 블록을 제거하고 외부 입력 스크립트만 사용
    //    float h = Input.GetAxisRaw("Horizontal");
    //    float v = Input.GetAxisRaw("Vertical");
    //    SetMoveInput(new Vector2(h, v));

    //    if (Input.GetKeyDown(KeyCode.E))
    //        RequestJump();

    //    if (Input.GetKeyDown(KeyCode.LeftShift))
    //        TryDash(transform.forward);

    //    if (Input.GetKeyDown(KeyCode.K))
    //    {
    //        // 테스트용 넉백
    //        AddImpulse((-transform.forward + Vector3.up) * 10f);
    //    }
    //}

    void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;

        UpdateGroundCheck();
        UpdateDashTimers(dt);
        UpdateHorizontalVelocity(dt);
        ApplyJumpIfRequested();
        ApplyGravity(dt);
        UpdateExternalImpulse(dt);
    }

    // ================== 외부 API ================== //

    /// <summary>
    /// 움직임 정지
    /// </summary>
    public void SetMovementLocked(bool locked)
    {
        movementLock = locked;
    }

    /// <summary>
    /// 입력 방향 설정 (XZ 평면)
    /// </summary>
    public void SetMoveInput(Vector2 input)
    {
        _moveInput = input;
        if (_moveInput.sqrMagnitude > 1f)
            _moveInput.Normalize();
    }

    /// <summary>
    /// 점프 요청 (Update에서 호출)
    /// 실제 점프 적용은 FixedUpdate에서 처리
    /// </summary>
    public void RequestJump()
    {
        _jumpRequested = true;
    }

    /// <summary>
    /// 대쉬 요청
    /// direction은 보통 캐릭터 forward 또는 입력 방향
    /// </summary>
    public void TryDash(Vector3 direction)
    {
        if (_isDashing)
            return;

        if (_dashCooldownTimer > 0f)
            return;

        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f)
            return;

        direction.Normalize();

        _isDashing = true;
        _dashTimer = dashDuration;
        _dashCooldownTimer = dashCooldown;
        _dashDirection = direction;
    }

    /// <summary>
    /// 넉백/스킬 등 외부 임펄스 추가
    /// </summary>
    public void AddImpulse(Vector3 impulse)
    {
        // 수평 임펄스는 별도 벡터에 누적
        Vector3 hImpulse = new Vector3(impulse.x, 0f, impulse.z);
        _externalHorizontalVelocity += hImpulse;

        // 수직 성분은 즉시 velocity에 반영
        Vector3 v = _rb.velocity;
        v.y += impulse.y;
        _rb.velocity = v;
    }

    // ================== 내부 로직 ================== //

    void UpdateGroundCheck()
    {
        _wasGrounded = _isGrounded;

        Vector3 origin = transform.position + Vector3.up * 0.1f;
        Ray ray = new Ray(origin, Vector3.down);
        float dist = groundCheckDistance + 0.1f;

        if (Physics.Raycast(ray, out RaycastHit hit, dist, groundMask, QueryTriggerInteraction.Ignore))
        {
            _isGrounded = true;
            _lastGroundedTime = Time.time;

            // 지면에 닿았으면 낙하 종료
            IsFalling = false;

            // 아래로 떨어지던 중이면 y 속도 정리
            Vector3 v = _rb.velocity;
            if (v.y < 0f)
            {
                v.y = 0f;
                _rb.velocity = v;
            }
        }
        else
        {
            _isGrounded = false;

            // 이전 프레임까지는 땅이었는데 지금은 공중이고,
            // y속도가 아래로 향하면 떨어지기 시작한 상태로 본다.
            Vector3 v = _rb.velocity;
            if (_wasGrounded && v.y <= 0f)
            {
                IsFalling = true;
            }
        }
    }

    void UpdateDashTimers(float dt)
    {
        if (_dashCooldownTimer > 0f)
            _dashCooldownTimer -= dt;

        if (_isDashing)
        {
            _dashTimer -= dt;
            if (_dashTimer <= 0f)
                _isDashing = false;
        }
    }

    void UpdateHorizontalVelocity(float dt)
    {
        Vector3 v = _rb.velocity;
        Vector3 horizontal = new Vector3(v.x, 0f, v.z);
        if (_isDashing)
        {
            // 대쉬 중에는 입력 무시, 고정 속도
            horizontal = _dashDirection * dashSpeed;
        }

        if (movementLock)
        {
            //v.x = 0;
            //v.z= 0;
            //_rb.velocity = v;
            //return;

            horizontal = Vector3.zero;
        }
        else
        {
            Vector3 desired = new Vector3(_moveInput.x, 0f, _moveInput.y) * moveSpeed;

            if (_moveInput.sqrMagnitude > 0f)
            {
                // 입력이 있을 때: 목표 속도까지 가속
                horizontal = Vector3.MoveTowards(horizontal, desired, acceleration * dt);

                // 공중에서만 약간 drag 적용
                if (!_isGrounded)
                {
                    horizontal = Vector3.Lerp(horizontal, Vector3.zero, airHorizontalDrag * dt);
                }
            }
            else
            {
                // 입력이 없을 때: 감속 + drag
                horizontal = Vector3.MoveTowards(horizontal, Vector3.zero, deceleration * dt);

                float drag = _isGrounded ? groundFriction : airHorizontalDrag;
                horizontal = Vector3.Lerp(horizontal, Vector3.zero, drag * dt);
            }
        }

        // 넉백 수평 성분 합산
        horizontal += new Vector3(_externalHorizontalVelocity.x, 0f, _externalHorizontalVelocity.z);

        v.x = horizontal.x;
        v.z = horizontal.z;
        _rb.velocity = v;
    }

    void ApplyJumpIfRequested()
    {
        if (movementLock)
            return;

        if (!_jumpRequested)
            return;

        _jumpRequested = false;

        bool canJump = _isGrounded || (Time.time - _lastGroundedTime <= coyoteTime);
        if (!canJump)
            return;

        Vector3 v = _rb.velocity;
        v.y = jumpPower;
        _rb.velocity = v;

        _isGrounded = false;
        IsFalling = false;
    }

    void ApplyGravity(float dt)
    {
        if (_isGrounded)
            return;

        Vector3 v = _rb.velocity;
        float newY = v.y + gravity * dt;
        if (newY < terminalVelocity)
            newY = terminalVelocity;

        v.y = newY;
        _rb.velocity = v;

        if (v.y <= 0f && !_isGrounded)
        {
            IsFalling = true;
        }
    }

    void UpdateExternalImpulse(float dt)
    {
        if (_externalHorizontalVelocity.sqrMagnitude <= 0.0001f)
        {
            _externalHorizontalVelocity = Vector3.zero;
            return;
        }

        _externalHorizontalVelocity = Vector3.Lerp(
            _externalHorizontalVelocity,
            Vector3.zero,
            knockbackDrag * dt
        );
    }
}
