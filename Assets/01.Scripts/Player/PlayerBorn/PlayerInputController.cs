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

        moveAction = playerInput.actions["Move"];
        lookAction = playerInput.actions["Look"];
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
        if (!character.IsGrounded) return;

        // 0이면 아예 대쉬 시작 못하게 막기
        if (stat == null || stat.curDashCount <= 0)
        {
            Debug.Log("대쉬카운트 없음");
            return;
        }

        Vector3 dir = (cam != null && moveWorld.sqrMagnitude > 0.0001f)
            ? moveWorld.normalized
            : transform.forward;

        combat?.CancelAttackForDash();

        bool dashStarted = (combat != null) && combat.TryDash(dir);
        if (!dashStarted) return;

        // 여기까지 왔으면 무조건 1개 소비
        stat.TryConsumeDash();

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

        combat?.TryStartParryStance();

        var parry = GetComponent<PlayerParry>();
        if (parry != null)
        {
            bool success = parry.TryConsumeInput();
            if (success)
                combat?.OnParrySuccess();
        }
    }


    public void OnToggleWeapon(InputAction.CallbackContext ctx)
    {
        if (isLocked) return;
        if (!ctx.started) return;
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
}
