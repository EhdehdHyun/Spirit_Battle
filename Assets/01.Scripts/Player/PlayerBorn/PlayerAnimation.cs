using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TextCore.Text;

public class PlayerAnimation : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private Rigidbody rb;
    [SerializeField] private PhysicsCharacter character;
    [SerializeField] private PlayerCombat combat;   
    [SerializeField] private GameObject weaponHitbox; 

    [Header("칼 활성화/비활성화")]
    [SerializeField] private GameObject weaponHand; //손에 칼
    [SerializeField] private GameObject weaponIn;   //집어넣은 칼

    public float runhold = 0.15f;

    private Animator anim;

    private static readonly int RunHash = Animator.StringToHash("Run");
    private static readonly int WalkHash = Animator.StringToHash("Walk");
    private static readonly int JumpHash = Animator.StringToHash("Jump");
    private static readonly int DashHash = Animator.StringToHash("Dash");
    private static readonly int ParryHash = Animator.StringToHash("Parry");
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

    public void SetIsAttacking(bool value)
    {
        anim.SetBool(IsAttackingHash, value);
    }
    public void SetWeaponEquipped(bool equipped)
    {
        anim.SetBool(WeaponEquippedHash, equipped);

        if (weaponHand != null)
        {
            weaponHand.SetActive(equipped);
        }

        if (weaponIn != null)
        {
            weaponIn.SetActive(equipped);
        }
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

    public void EvParryWindowOpen()
    {
        combat?.EvParryWindowOpen();
    }

    public void EvParryWindowClose()
    {
        combat.EvParryWindowClose();
    }

    public void EvParryEnd()
    {
        combat.EvParryEnd();
    }

    }



