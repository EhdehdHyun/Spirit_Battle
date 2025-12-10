using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PhysicsCharacter))]
public class PlayerInputController : MonoBehaviour
{
    public PlayerAnimation anime;
    public ThirdPersonCamera cam;
    public float faceTurnSpeed = 18f;

    private PhysicsCharacter character;

    private Vector2 moveRaw;
    private Vector2 lookRaw;

    private Vector3 moveWorld;
    private Quaternion targetRot;

    private void Awake()
    {
        character = GetComponent<PhysicsCharacter>();
    }

    private void Update()
    {
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
        if (moveWorld.sqrMagnitude > 0.0001f)
        {
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRot,
                faceTurnSpeed * Time.fixedDeltaTime
            );
        }
    }

    public void OnMove(InputAction.CallbackContext ctx)
    {
        moveRaw = ctx.ReadValue<Vector2>();
        if (moveRaw.magnitude < 0.05f) moveRaw = Vector2.zero; // 입력 노이즈 컷
        else if (moveRaw.sqrMagnitude > 1f) moveRaw.Normalize();
    }

    public void OnLook(InputAction.CallbackContext ctx)
    {
        lookRaw = ctx.ReadValue<Vector2>();
    }

    public void OnJump(InputAction.CallbackContext ctx)
    {
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

        Vector3 dir = (cam != null && moveWorld.sqrMagnitude > 0.0001f)
            ? moveWorld.normalized
            : transform.forward;

        character.TryDash(dir);
    }

    public void OnAttackFire()
    {

    } 
}
