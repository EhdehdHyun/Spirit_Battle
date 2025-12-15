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
    public float faceTurnSpeed = 18f;

    private InputAction moveAction;
    private InputAction lookAction;

    private Vector2 moveRaw;
    private Vector2 lookRaw;

    private Vector3 moveWorld;
    private Quaternion targetRot;


    public bool isLocked = false;
    private void Awake()
    {
        character = GetComponent<PhysicsCharacter>();
        anime = GetComponent<PlayerAnimation>();
        combat = GetComponent<PlayerCombat>();
        playerInput = GetComponent<PlayerInput>();

        moveAction = playerInput.actions["Move"];
        lookAction = playerInput.actions["Look"];
    }

    private void Update()
    {
        if (isLocked)
        {
            character.SetMoveInput(Vector2.zero);
            if(cam != null )
            {
                cam.SetLookInput(Vector2.zero);
            }
            return;
        }

        // 매 프레임 현재 입력 상태를 다시 읽음 (대쉬 후 입력 다시 누를 필요 없어짐)
        moveRaw = moveAction.ReadValue<Vector2>();
        lookRaw = lookAction.ReadValue<Vector2>();

        // 노이즈 컷
        if (moveRaw.magnitude < 0.05f) moveRaw = Vector2.zero;
        else if (moveRaw.sqrMagnitude > 1f) moveRaw.Normalize();

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

        if (moveWorld.sqrMagnitude > 0.0001f)
        {
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRot,
                faceTurnSpeed * Time.fixedDeltaTime
            );
        }
    }

    //public void OnMove(InputAction.CallbackContext ctx)
    //{
    //    if(isLocked ) { moveRaw = Vector2.zero; return; }

    //    moveRaw = ctx.ReadValue<Vector2>();
    //    if (moveRaw.magnitude < 0.05f) moveRaw = Vector2.zero; // 입력 노이즈 컷
    //    else if (moveRaw.sqrMagnitude > 1f) moveRaw.Normalize();
    //}

    //public void OnLook(InputAction.CallbackContext ctx)
    //{
    //    if(isLocked)
    //    {
    //        lookRaw = Vector2.zero;
    //        return;
    //    }

    //    lookRaw = ctx.ReadValue<Vector2>();
    //}

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
        if(isLocked) return;

        if (!ctx.performed) return;

        Vector3 dir = (cam != null && moveWorld.sqrMagnitude > 0.0001f)
            ? moveWorld.normalized
            : transform.forward;

        if (combat.TryDash(dir)) return;

        character.TryDash(dir);
        anime.PlayDash();
    }

    public void OnAttack(InputAction.CallbackContext ctx)
    {
        if (isLocked) return;

        if (!ctx.started) return;
        combat?.OnAttackInput();
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
}
