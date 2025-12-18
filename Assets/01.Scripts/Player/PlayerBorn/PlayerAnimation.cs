using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.TextCore.Text;

public class PlayerAnimation : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private Rigidbody rb;
    [SerializeField] private PhysicsCharacter character;
    [SerializeField] private PlayerCombat combat;   
    [SerializeField] private GameObject weaponHitbox; 

    [Header("무기")]
    [SerializeField] private ParentConstraint weaponParent; 
    [SerializeField] private int handSourceIndex = 0;   //손에 칼
    [SerializeField] private int sideSourceIndex = 1;   //옆에 칼

    [Header("걷기 기준")]
    public float runhold = 0.15f;

    private Animator anim;

    private static readonly int RunHash = Animator.StringToHash("Run");
    private static readonly int WalkHash = Animator.StringToHash("Walk");
    private static readonly int JumpHash = Animator.StringToHash("Jump");
    private static readonly int DashHash = Animator.StringToHash("Dash");
    private static readonly int ParryHash = Animator.StringToHash("Parry");
    private static readonly int DeadHash = Animator.StringToHash("Dead");
    private static readonly int GroundedHash = Animator.StringToHash("Grounded");

    private static readonly int WeaponEquippedHash = Animator.StringToHash("WeaponEquipped");
    private static readonly int ComboIndexHash = Animator.StringToHash("ComboIndex");
    private static readonly int AttackHash = Animator.StringToHash("Attack");
    private static readonly int IsAttackingHash = Animator.StringToHash("IsAttacking");

    private void Awake()
    {
        anim = GetComponent<Animator>();
        if (rb == null) rb = GetComponent<Rigidbody>();
        if (character == null) character = GetComponent<PhysicsCharacter>();
        if (combat == null) combat = GetComponent<PlayerCombat>();
    }

    private void Update()
    {
        if (rb == null) return;

        if (character.IsDashing)
        {
            anim.SetBool(WalkHash, false);
            anim.SetBool(RunHash, false);
        }

        Vector3 v = rb.velocity;
        v.y = 0f;

        // 수평 속도가 일정 이상이면 달리기
        bool moving = v.magnitude > runhold;
        bool running = (character != null && character.IsRunning);
        anim.SetBool(RunHash, running);
        anim.SetBool(WalkHash, moving && !running);

        if (character != null)
            anim.SetBool(GroundedHash, character.IsGrounded);
    }

    public void PlayJump()
    {
        anim.SetTrigger(JumpHash);
    }

    public void PlayDash()
    {
        anim.SetTrigger(DashHash);
    }

    public void PlayParry()
    {
        anim.SetTrigger(ParryHash);
    }

    public void Attack(int comboIndex)
    {
        anim.SetInteger(ComboIndexHash, comboIndex);
        anim.SetTrigger(AttackHash);
    }

    public void PlayIdle()
    {
        anim.CrossFade("Idle_Anim", 0.1f);
    }

    public void PlayDie()
    {
        anim.SetTrigger(DeadHash);
    }

    public void SetIsAttacking(bool value)
    {
        anim.SetBool(IsAttackingHash, value);
    }

    public void SetWeaponEquipped(bool equipped)
    {
        anim.SetBool(WeaponEquippedHash, equipped);
    }

    private void SetSourceWeight(ParentConstraint pc, int index, float w)
    {
        if (!pc) return;
        if (index < 0 || index >= pc.sourceCount) return;

        var src = pc.GetSource(index);  //ConstraintSource
        src.weight = Mathf.Clamp01(w);
        pc.SetSource(index, src);
    }
    //===== 애니메이션 이벤트 =====
    public void EvAttackMoveForward()   //앞으로 전진
    {
        if (combat != null)
            combat.OnAttackMoveForwardFromAnim();
    }

    public void EvAttackStepBack()  //뒤로 후퇴
    {
        if (combat != null)
            combat.OnAttackStepBackFromAnim();
    }

    public void EvAttackAnimationEnd()  //공격애니메이션 끝 알림
    {
        if (combat != null)
            combat.OnAttackAnimationEndFromAnim();
    }

    public void EvAttackHitboxDisable() //weaponHit 끄기
    {
        if (weaponHitbox != null)
            weaponHitbox.SetActive(false);
    }

    public void EvAttackHitboxEnable()  //weaponhit 켜기
    {
        if (weaponHitbox != null)
            weaponHitbox.SetActive(true);
    }
    public void EvWeaponToHand()
    {
        if (!weaponParent) return;

        weaponParent.constraintActive = true;
        weaponParent.weight = 1f;

        SetSourceWeight(weaponParent, handSourceIndex, 1f);
        SetSourceWeight(weaponParent, sideSourceIndex, 0f);
    }

    public void EvWeaponToSide()
    {
        if (!weaponParent) return;

        weaponParent.constraintActive = true;
        weaponParent.weight = 1f;

        SetSourceWeight(weaponParent, handSourceIndex, 0f);
        SetSourceWeight(weaponParent, sideSourceIndex, 1f);
    }
}



