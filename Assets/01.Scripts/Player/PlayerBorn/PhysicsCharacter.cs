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
    [Tooltip("달리기 목표 수평 이동 속도")]
    public float runSpeed = 8f;

    [Tooltip("걷기 목표 수평 이동 속도")]
    public float walkSpeed = 4f;

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

    [Header("넉백")]
    [Tooltip("넉백 수평 속도가 줄어드는 정도")]
    public float knockbackDrag = 10f;

    [Header("지면 체크")]
    [Tooltip("발밑 지면 체크 거리")]
    public float groundCheckDistance = 0.3f;

    [Tooltip("지면/벽으로 인식할 레이어 마스크")]
    public LayerMask groundMask;

    [Header("경사/붙이기")]
    [Tooltip("이 각도보다 가파르면 지면으로 인정하지 않음")]
    public float slopeLimit = 55f;

    [Tooltip("바닥에 붙이는 힘")]
    public float groundStickForce = 30f;

    [Tooltip("캡슐 캐스트 반지름에 적용할 스킨")]
    [Range(0.7f, 1.0f)] public float groundCastSkin = 0.95f;

    [Tooltip("지면 체크 여유 거리")]
    public float groundCastExtra = 0.05f;

    [Header("벽 슬라이드 (벽에 달라붙는 현상 방지)")]
    [Tooltip("벽 체크 거리")]
    public float wallCheckDistance = 0.2f;

    [Tooltip("이 값보다 normal.y가 작으면 벽/수직면으로 판단")]
    [Range(0f, 1f)] public float wallNormalYThreshold = 0.2f;

    [Tooltip("미세한 접촉에서 떨림 방지용")]
    public float wallStickEpsilon = 0.01f;

    [Header("이동 정지")]
    public bool movementLock = false;

    // 외부 조회용 프로퍼티
    public Vector3 Velocity => _rb.velocity;
    public bool IsGrounded => _isGrounded;
    public bool IsDashing => _isDashing;
    public bool IsFalling { get; private set; }
    public bool IsRunning =>
         !_isDashing &&
         !movementLock &&
         _moveInput.sqrMagnitude > 0.001f &&
         (
             (_weaponEquipped && _canCombatRun) ||
             (!_weaponEquipped && _runAfterDash)
         );

    // 내부 필드
    Rigidbody _rb;
    CapsuleCollider _col; // 캡슐 기반 지면판정용

    // 입력 벡터 (XZ)
    Vector2 _moveInput;

    // 지면 상태
    bool _isGrounded;
    bool _wasGrounded;
    float _lastGroundedTime;

    // 현재 지면 노멀/히트 정보(경사 투영 정보)
    Vector3 _groundNormal = Vector3.up;
    RaycastHit _groundHit;

    //달리기 상태
    bool _runAfterDash;
    bool _weaponEquipped;
    bool _runHeld;

    // 대쉬 상태
    bool _isDashing;
    float _dashTimer;
    float _dashCooldownTimer;
    Vector3 _dashDirection;
    bool _airDashUsed;

    // 점프 입력 버퍼
    bool _jumpRequested;

    // 외부 수평 임펄스 (넉백 등)
    Vector3 _externalHorizontalVelocity;

    //스탯 관련
    PlayerStat _stat;
    bool _canCombatRun = true;
    void Awake()
    {
        _stat = GetComponent<PlayerStat>();
        _rb = GetComponent<Rigidbody>();
        _col = GetComponent<CapsuleCollider>();

        _rb.isKinematic = false;
        _rb.useGravity = false;
        _rb.constraints = RigidbodyConstraints.FreezeRotationX
                        | RigidbodyConstraints.FreezeRotationY
                        | RigidbodyConstraints.FreezeRotationZ;
        _rb.interpolation = RigidbodyInterpolation.Interpolate;

        _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
    }

    void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;

        if (_runAfterDash && !_isDashing && _moveInput.sqrMagnitude <= 0.001f)
            _runAfterDash = false;

        UpdateGroundCheck();
        UpdateDashTimers(dt);

        bool wantsRunMode = !_isDashing && !movementLock && _moveInput.sqrMagnitude > 0.001f && (_weaponEquipped || _runAfterDash);
        if (_weaponEquipped && _stat != null)
            _canCombatRun = _stat.TickCombatRunStamina(inCombat: true, isTryingRun: (_runHeld || _runAfterDash), dt: dt);
        else
            _canCombatRun = true;

        if (_weaponEquipped)
            _canCombatRun = _canCombatRun && (_runHeld || _runAfterDash);

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
    /// 무기장착 요청
    /// </summary>
    public void SetWeaponEquipped(bool equipped)
    {
        _weaponEquipped = equipped;
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
    public bool TryDash(Vector3 direction, bool allowAirDash, bool allowWhileDashing)
    {
        if (_isDashing && !allowWhileDashing)
            return false;

        if (!IsGrounded && !allowAirDash)
            return false;

        if (!IsGrounded)
        {
            if (!allowAirDash) return false;
            if (_airDashUsed) return false; // 공중 대쉬는 1회만(원하면 여기 바꿔)
            _airDashUsed = true;
        }

        // 지상 대쉬는 경사면 투영
        if (IsGrounded)
            direction = Vector3.ProjectOnPlane(direction, _groundNormal);
        else
            direction.y = 0f;

        if (direction.sqrMagnitude < 0.0001f)
            return false;

        direction.Normalize();

        _isDashing = true;
        _dashTimer = dashDuration;
        _dashDirection = direction;

        _runAfterDash = true;
        return true;
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

    public void SetRunHeld(bool held)
    {
        _runHeld = held;

        if (!_weaponEquipped)
            _runAfterDash = held;
    }

    // ================== 내부 로직 ================== //

    // 경사면 투영 유틸
    static Vector3 ProjectOnGround(Vector3 dir, Vector3 groundNormal)
    {
        dir = Vector3.ProjectOnPlane(dir, groundNormal);
        return dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector3.zero;
    }

    // 캡슐 월드 포인트 계산
    void GetCapsuleWorldPoints(out Vector3 p1, out Vector3 p2, out float radius)
    {
        Vector3 center = transform.TransformPoint(_col.center);

        float scaleX = Mathf.Abs(transform.lossyScale.x);
        float scaleY = Mathf.Abs(transform.lossyScale.y);
        float scaleZ = Mathf.Abs(transform.lossyScale.z);

        radius = _col.radius * Mathf.Max(scaleX, scaleZ);
        float height = Mathf.Max(_col.height * scaleY, radius * 2f);

        float half = height * 0.5f;
        float offset = half - radius;

        Vector3 up = Vector3.up;
        p1 = center + up * offset;
        p2 = center - up * offset;
    }

    Vector3 RemoveIntoWallVelocity(Vector3 horizontal)
    {
        if (horizontal.sqrMagnitude <= 0.0001f) return horizontal;

        GetCapsuleWorldPoints(out Vector3 p1, out Vector3 p2, out float radius);

        Vector3 dir = horizontal.normalized;
        float castRadius = radius * groundCastSkin;

        // 이동하는 방향으로 캡슐캐스트해서 "바로 앞의 벽"만 검사
        if (Physics.CapsuleCast(
            p1, p2,
            castRadius,
            dir,
            out RaycastHit hit,
            wallCheckDistance,
            groundMask,
            QueryTriggerInteraction.Ignore
        ))
        {
            // 노멀이 거의 수평이면(= y가 작으면) 벽으로 판단
            if (Mathf.Abs(hit.normal.y) < wallNormalYThreshold)
            {
                // 벽 안으로 파고드는 성분 제거 + 평면 투영(슬라이드)
                float dot = Vector3.Dot(horizontal, hit.normal);

                // dot이 음수면(대부분) 벽 쪽으로 파고드는 중이므로 그 성분만 제거
                if (dot < -wallStickEpsilon)
                    horizontal -= hit.normal * dot;

                horizontal = Vector3.ProjectOnPlane(horizontal, hit.normal);
            }
        }

        return horizontal;
    }

    void UpdateGroundCheck()
    {
        _wasGrounded = _isGrounded;

        GetCapsuleWorldPoints(out Vector3 p1, out Vector3 p2, out float radius);

        float castRadius = radius * groundCastSkin;
        float dist = groundCheckDistance + groundCastExtra;        

        bool hit = Physics.CapsuleCast(
            p1, p2,
            castRadius,
            Vector3.down,
            out _groundHit,
            dist,
            groundMask,
            QueryTriggerInteraction.Ignore
        );

        if (hit)
        {
            float angle = Vector3.Angle(_groundHit.normal, Vector3.up);

            // 너무 가파른 면은 벽/미끄럼 취급
            if (angle <= slopeLimit)
            {
                _isGrounded = true;
                _groundNormal = _groundHit.normal; // 이동/대쉬 투영에 사용
                _lastGroundedTime = Time.time;

                IsFalling = false;

                // 땅 밟으면 에어대쉬 사용 상태 리셋
                _airDashUsed = false;

                // 아래로 떨어지던 중이면 y 속도 정리(착지 튐 방지)
                Vector3 v = _rb.velocity;
                if (v.y < 0f)
                {
                    v.y = 0f;
                    _rb.velocity = v;
                }
                return;
            }
        }

        _isGrounded = false;
        _groundNormal = Vector3.up;

        // 이전 프레임까지는 땅이었는데 지금은 공중이고,
        // y속도가 아래로 향하면 떨어지기 시작한 상태로 본다.
        Vector3 vv = _rb.velocity;
        if (_wasGrounded && vv.y <= 0f)
        {
            IsFalling = true;
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

    //Walk적용
    void UpdateHorizontalVelocity(float dt)
    {
        Vector3 v = _rb.velocity;
        Vector3 horizontal = new Vector3(v.x, 0f, v.z);

        if (movementLock)
        {
            horizontal = Vector3.zero;
        }

        if (_isDashing)
        {
            // 대쉬 중에는 입력 무시, 고정 속도
            Vector3 dashDir = _dashDirection;

            // 지상 대쉬는 경사면 평면을 따라감
            if (_isGrounded)
                dashDir = ProjectOnGround(dashDir, _groundNormal);

            horizontal = dashDir * dashSpeed;
        }
        else
        {
            bool runMode = (_weaponEquipped || _runAfterDash);

            if (_weaponEquipped && !_canCombatRun)
                runMode = false;

            float targetSpeed = (_weaponEquipped || _runAfterDash) ? runSpeed : walkSpeed;

            if (_weaponEquipped)
            {
                if (!(_runHeld || _runAfterDash) || !_canCombatRun)
                    targetSpeed = walkSpeed;
            }
            else
            {
                if (_runHeld || _runAfterDash)
                    targetSpeed = runSpeed;
            }

            // 입력 벡터를 "지면 평면"에 맞춰서 이동
            Vector3 inputDir = new Vector3(_moveInput.x, 0f, _moveInput.y);
            if (inputDir.sqrMagnitude > 0.0001f)
                inputDir.Normalize();

            if (_isGrounded)
                inputDir = ProjectOnGround(inputDir, _groundNormal);

            Vector3 desired = inputDir * targetSpeed;

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

        horizontal = RemoveIntoWallVelocity(horizontal);

        v.x = horizontal.x;
        v.z = horizontal.z;
        _rb.velocity = v;

        // 바닥 붙이기
        if (_isGrounded && !_isDashing && !_jumpRequested)
        {
            _rb.AddForce(-_groundNormal * groundStickForce, ForceMode.Acceleration);
        }
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
