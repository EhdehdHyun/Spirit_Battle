using UnityEngine.InputSystem;
using UnityEngine;

[RequireComponent(typeof(PhysicsCharacter))]
public class PlayerInputController : MonoBehaviour
{
    private PhysicsCharacter _character;

    private void Awake()
    {
        _character = GetComponent<PhysicsCharacter>();
    }

    public void OnMove(InputAction.CallbackContext context)
    {

    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            _character.RequestJump();
        }
    }

    public void OnDash(InputAction.CallbackContext context)
    {

    }

    public void OnRolling(InputAction.CallbackContext context)
    {

    }

    public void OnInteract(InputAction.CallbackContext context)
    {

    }
}
