using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TextCore.Text;

public class PlayerAnimation : MonoBehaviour
{
    public Rigidbody rb;
    public PhysicsCharacter character;
    public float runhold = 0.15f;

    private Animator anim;

    private static readonly int RunHash = Animator.StringToHash("Run");
    private static readonly int JumpHash = Animator.StringToHash("Jump");
    private static readonly int GroundedHash = Animator.StringToHash("Grounded");

    private void Awake()
    {
        anim = GetComponent<Animator>();
        if (rb == null) rb = GetComponent<Rigidbody>();
        if (character == null) character = GetComponent<PhysicsCharacter>();
    }

    private void Update()
    {
        if (rb == null) return;

        Vector3 v = rb.velocity;
        v.y = 0f;

        // 수평 속도가 일정 이상이면 달리기
        bool isRunning = v.magnitude > runhold;
        anim.SetBool(RunHash, isRunning);

        if (character != null)
            anim.SetBool(GroundedHash, character.IsGrounded);
    }

    public void PlayJump()
    {
        anim.SetTrigger(JumpHash);
    }
}
