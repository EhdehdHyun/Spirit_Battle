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


    public float faceTurnSpeed = 18f;

    private InputAction moveAction;
    private InputAction lookAction;

    private InputAction runAction;
    private bool runHeld;

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
        runAction = playerInput.actions["Run"];
    }

    private void Update()
    {
        // 매 프레임 현재 입력 상태를 다시 읽음 (대쉬 후 입력 다시 누를 필요 없어짐)
        moveRaw = moveAction.ReadValue<Vector2>();
        lookRaw = lookAction.ReadValue<Vector2>();
        runHeld = runAction != null && runAction.IsPressed();
        character.SetRunHeld(runHeld);

        // 노이즈 컷
        if (moveRaw.magnitude < 0.05f) moveRaw = Vector2.zero;
        else if (moveRaw.sqrMagnitude > 1f) moveRaw.Normalize();

        if (isLocked)
        {
            character.SetMoveInput(Vector2.zero);
            if(cam != null )
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

        if(IsParrying()) return;

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

        // 1) 2번째 대쉬 (윈도우 안)
        if (stat.CanSecondDashNow)
        {
            // 2번째는 "대쉬 중 재시작"이 가능해야 함 -> PlayerCombat/PhysicsCharacter 수정 필요(아래 참고)
            bool startedSecond = (combat != null) && combat.TryDash(dir, airDashAllowed, allowWhileDashing: true);
            if (!startedSecond) return;

            stat.CommitSecondDashUsed(); // 여기서 1초 쿨 시작(스태미나 추가 소모 없음)
            SetDashLock(character.dashDuration);
            return;
        }

        // 2) 첫 대쉬 (새 “대쉬 사용” 시작)
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
        if (isLocked) return;

        if (!ctx.started) return;
        combat?.OnAttackInput();
    }

    public void OnParry(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed) return;
        if (isLocked) return;
        if (IsParrying()) return;

        // 입력 순간엔 "자세 시작"만.
        // 실제 패링 판정은 PlayerParry.Anim_TryParryNow() (애니 이벤트)에서 함.
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
        if(cam != null) 
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
        if (!ctx.performed) return;
        if (isLocked) return;
        if (IsParrying()) return;

        combat?.OnSkill1Input();
    }

}
