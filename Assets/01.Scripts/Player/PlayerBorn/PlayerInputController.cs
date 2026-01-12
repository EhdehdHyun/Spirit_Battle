using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PhysicsCharacter))]
public class PlayerInputController : MonoBehaviour
{
    public ThirdPersonCamera cam;
    private PhysicsCharacter character;
    public PlayerAnimation anime;
    public PlayerInput playerInput;
    public PlayerCombat combat;
    public PlayerStat stat;
    public PlayerParry parry;
    public PlayerAbility ability;
    [SerializeField] private PlayerWhirlwindSkill whirlwind;
    [SerializeField] private PlayerTornadoSkill tornado;


    public float faceTurnSpeed = 18f;

    private InputAction moveAction;
    private InputAction lookAction;

    private Vector2 moveRaw;
    private Vector2 lookRaw;

    private Vector3 moveWorld;
    private Quaternion targetRot;

    //완전 행동 정지
    public bool isLocked = false;
    //대쉬 전용 정지
    public bool dashLocked = false;
    public Coroutine dashLockCo;
    private void Awake()
    {
        character = GetComponent<PhysicsCharacter>();
        anime = GetComponent<PlayerAnimation>();
        combat = GetComponent<PlayerCombat>();
        playerInput = GetComponent<PlayerInput>();
        stat = GetComponent<PlayerStat>();
        parry = GetComponent<PlayerParry>();
        ability = GetComponent<PlayerAbility>();

        moveAction = playerInput.actions["Move"];
        lookAction = playerInput.actions["Look"];

        if (whirlwind == null)
            whirlwind = GetComponent<PlayerWhirlwindSkill>();

        if (tornado == null)
            tornado = GetComponent<PlayerTornadoSkill>();
    }

    private void Update()
    {
        // 매 프레임 현재 입력 상태를 다시 읽음 (대쉬 후 입력 다시 누를 필요 없어짐)
        moveRaw = moveAction.ReadValue<Vector2>();
        lookRaw = lookAction.ReadValue<Vector2>();

        // 노이즈 컷
        if (moveRaw.magnitude < 0.05f) moveRaw = Vector2.zero;
        else if (moveRaw.sqrMagnitude > 1f) moveRaw.Normalize();

        if (isLocked)
        {
            character.SetMoveInput(Vector2.zero);
            if (cam != null)
            {
                cam.SetLookInput(Vector2.zero);
            }
            return;
        }
        if (cam != null)
            cam.SetLookInput(lookRaw);

        if (cam != null)
        {
            moveWorld =
                cam.PlanarForward * moveRaw.y +
                cam.PlanarRight * moveRaw.x;

            character.SetMoveInput(new Vector2(moveWorld.x, moveWorld.z));

            if (moveWorld.sqrMagnitude > 0.0001f)
                targetRot = Quaternion.LookRotation(moveWorld, Vector3.up);
        }
        else
        {
            character.SetMoveInput(moveRaw);
            moveWorld = Vector3.zero;
        }
    }

    private void FixedUpdate()
    {
        if (isLocked) return;

        if (character.movementLock) return;

        if (dashLocked || character.IsDashing) return;

        if (moveWorld.sqrMagnitude > 0.0001f)
        {
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRot,
                faceTurnSpeed * Time.fixedDeltaTime
            );
        }
    }

    public void OnJump(InputAction.CallbackContext ctx)
    {
        if (isLocked || character.movementLock) return;

        if (IsParrying()) return;

        if (ctx.performed)
        {
            character.RequestJump();

            if (anime != null)
            {
                if (character.IsGrounded)
                    anime.PlayJump();
            }
        }
    }

    public void OnDash(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed) return;
        if (isLocked) return;
        if (IsParrying()) return;

        whirlwind?.CancelByDash();
        tornado?.CancelByDash();

        bool airDashAllowed = (ability != null && ability.Has(AbilityId.AirDash));
        if (!character.IsGrounded && !airDashAllowed) return;

        if (stat == null)
        {
            Debug.LogWarning("PlayerStat 없음");
            return;
        }

        Vector3 dir = (cam != null && moveWorld.sqrMagnitude > 0.0001f)
            ? moveWorld.normalized
            : transform.forward;

        combat?.CancelAttackForDash();

        if (stat.CanSecondDashNow)
        {
            bool startedSecond = (combat != null) && combat.TryDash(dir, airDashAllowed, allowWhileDashing: true);
            if (!startedSecond) return;

            stat.CommitSecondDashUsed(); // 1초 쿨 시작(스태미나 추가 소모 없음)
            SetDashLock(character.dashDuration);
            return;
        }

        // 첫 대쉬 (새 “대쉬 사용” 시작)
        if (!stat.CanStartDashUse())
        {
            // 쿨타임이거나 스태미나 부족
            return;
        }

        bool startedFirst = (combat != null) && combat.TryDash(dir, airDashAllowed, allowWhileDashing: false);
        if (!startedFirst) return;

        // 첫 대쉬가 실제로 시작됐으니: 스태미나 15 소모 + 2번째 입력 윈도우 오픈
        if (!stat.CommitDashUseStart())
        {
            Debug.LogWarning("대쉬 시작됐는데 스태미나 커밋 실패(체크/커밋 타이밍 꼬임)");
            return;
        }

        SetDashLock(character.dashDuration);
    }



    public void OnAttack(InputAction.CallbackContext ctx)
    {
        if (character != null && character.movementLock) return;

        if (isLocked) return;

        if (!ctx.started) return;
        combat?.OnAttackInput();
    }

    public void OnParry(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed) return;
        if (isLocked) return;
        if (IsParrying()) return;

        combat?.TryStartParryStance();
    }


    public void OnToggleWeapon(InputAction.CallbackContext ctx)
    {
        if (isLocked) return;
        if (IsParrying()) return;
        if (!ctx.started) return;
        if (character.IsDashing) return;
        if (dashLocked) return;
        combat?.OnToggleWeaponInput();
    }

    public void Lock()
    {
        isLocked = true;
        if (cam != null)
            cam.SetLookInput(Vector2.zero);
    }

    public void Unlock()
    {
        isLocked = false;
    }

    public void SetDashLock(float duration)
    {
        dashLocked = true;
        if (dashLockCo != null) StopCoroutine(dashLockCo);
        dashLockCo = StartCoroutine(DashLockRoutine(duration));
    }
    public void ResetInputState()
    {
        moveRaw = Vector2.zero;
        lookRaw = Vector2.zero;

        moveWorld = Vector3.zero;

        // 회전 기준을 현재 바라보는 방향으로 강제 동기화
        targetRot = Quaternion.Euler(0, transform.eulerAngles.y, 0);

        character.SetMoveInput(Vector2.zero);

        dashLocked = false;
        if (dashLockCo != null)
        {
            StopCoroutine(dashLockCo);
            dashLockCo = null;
        }
    }

    private IEnumerator DashLockRoutine(float duration)
    {
        yield return new WaitForSeconds(duration);
        dashLocked = false;
        dashLockCo = null;
    }
    private bool IsParrying()
    {
        return parry != null && parry.isParryStance;
    }

    public void OnSkill1(InputAction.CallbackContext ctx)
    {
        if (character != null && character.movementLock) return;

        if (!ctx.performed) return;
        if (isLocked) return;
        if (IsParrying()) return;

        combat?.OnSkill1Input();
    }

    public void OnWhirlwind(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed) return;
        if (isLocked) return;
        if (character != null && character.movementLock) return;
        if (IsParrying()) return;

        whirlwind?.OnSkillInput();
    }

    public void OnTornado(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed) return;
        if (isLocked) return;
        if (IsParrying()) return;

        if (character != null && character.IsDashing) return; // 대쉬 중엔 시전 시작 불가능하게 함
        if (character != null && character.movementLock) return; // 시전 중 중복 입력 방지함

        tornado?.OnSkillInput();
    }
    public void ForceIdle()
    {
        // 입력 완전 초기화
        moveRaw = Vector2.zero;
        lookRaw = Vector2.zero;
        moveWorld = Vector3.zero;

        // 이동 입력 제거
        character.SetMoveInput(Vector2.zero);

        // 회전 기준 현재 방향으로 고정
        targetRot = Quaternion.Euler(0, transform.eulerAngles.y, 0);

        // 락 상태 해제
        dashLocked = false;
    }
}
