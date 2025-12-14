using UnityEngine;

public class PlayerCombat : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private PlayerAnimation playerAnim;
    [SerializeField] private PhysicsCharacter physicsCharacter;
    [SerializeField] private PlayerInputController playerInput;
    [SerializeField] private WeaponHitBox weaponHitBox;

    [Header("콤보 설정")]
    [SerializeField] private int maxCombo = 5;
    //[SerializeField] private float comboInputWindow = 0.4f;

    [Header("기본 공격력 (스탯/무기 공격력")]
    [SerializeField] private float baseDamage = 10f;

    [Header("콤보별 데미지")]
    [SerializeField] private float[] comboDamages = { 1.0f, 1.2f, 1.5f, 1.8f, 2.0f };

    [Header("공격 전진 힘")]
    [SerializeField] private float[] forwardPowers = { 1f, 0.5f, 1f, 0f, 3f };

    [Header("공격 후퇴 힘")]
    [SerializeField] private float[] backwardPowers = { 0f, 0f, 0f, 2.5f, 0f };

    [Header("무기 상태")]
    [SerializeField] private bool weaponEquipped = false;
    public bool WeaponEquipped => weaponEquipped;

    int currentCombo = 0;
    bool isAttacking = false;
    bool bufferedNextInput = false;
    float lastAttackTime = 0f;

    //public bool attackMovingLock = false;

    void Awake()
    {
        if (playerAnim == null)
            playerAnim = GetComponentInChildren<PlayerAnimation>();
        if (physicsCharacter == null)
            physicsCharacter = GetComponent<PhysicsCharacter>();
        if(playerInput == null)
            playerInput = GetComponent<PlayerInputController>();  
        if(weaponHitBox == null)
            weaponHitBox = GetComponentInChildren<WeaponHitBox>();
    }


    public void OnAttackInput()
    {
        if (!weaponEquipped) return;   // 무기 안 들면 공격 안됨

        if(playerInput.isLocked) return;

        if (!isAttacking)
        {
            StartFirstAttack();
            return;
        }

        if (bufferedNextInput) return;

        //if (Time.time - lastAttackTime <= comboInputWindow)
            bufferedNextInput = true;
    }

    public void OnToggleWeaponInput()
    {
        weaponEquipped = !weaponEquipped;
        playerAnim?.SetWeaponEquipped(weaponEquipped);

        if (!weaponEquipped && isAttacking)
            ForceStopAttack();
    }


    void StartFirstAttack()
    {
        isAttacking = true;
        bufferedNextInput = false;

        currentCombo = 1;
        lastAttackTime = Time.time;

        //attackMovingLock = true;
        physicsCharacter.SetMovementLocked(true);

        playerAnim?.Attack(currentCombo);
        playerAnim?.SetIsAttacking(true);
    }

    void StartNextAttack()
    {
        if (currentCombo >= maxCombo)
        {
            ResetCombo();
            return;
        }

        currentCombo++;
        bufferedNextInput = false;
        lastAttackTime = Time.time;

        //attackMovingLock = true;
        physicsCharacter.SetMovementLocked(true);

        playerAnim?.Attack(currentCombo);
    }

    void ResetCombo()
    {
        isAttacking = false;
        bufferedNextInput = false;
        currentCombo = 0;

        //attackMovingLock = false;
        physicsCharacter.SetMovementLocked(false);

        playerAnim?.SetIsAttacking(false);
        playerAnim.PlayIdle();

    }

    void ForceStopAttack()
    {
        ResetCombo();
    }


    public void OnAttackAnimationEndFromAnim()
    {
        if (weaponEquipped && bufferedNextInput && currentCombo < maxCombo)
        {
            StartNextAttack();
            return;
        }

        ResetCombo();
    }

    public void OnAttackMoveForwardFromAnim()
    {
        if (physicsCharacter == null)
        {
            return;
        }
        if (currentCombo <= 0 || currentCombo > forwardPowers.Length) return;

        float power = forwardPowers[currentCombo - 1];
        if (power <= 0f) 
        {
            return; 
        }

        Vector3 dir = transform.forward;
        dir.y = 0f;
        dir.Normalize();

        physicsCharacter.AddImpulse(dir * power);
    }

    public void OnAttackStepBackFromAnim()
    {
        if (physicsCharacter == null) return;
        if (currentCombo <= 0 || currentCombo > backwardPowers.Length)
        {
            return;
        }

        float power = backwardPowers[currentCombo - 1];
        if (power <= 0f) 
        {

            return;
        } 

        Vector3 dir = -transform.forward;
        dir.y = 0f;
        dir.Normalize();

        physicsCharacter.AddImpulse(dir * power);
    }

    public void OnAttackHitStartFromAnim()
    {
        if (weaponHitBox == null) return;
        if (currentCombo <= 0 ) return;

        int idx = Mathf.Clamp(currentCombo - 1, 0, comboDamages.Length - 1);

        float multiplier = comboDamages[idx];
        float finalDamage = baseDamage * multiplier;

        weaponHitBox.Activate(finalDamage);
    }

    public void OnAttackHitEndFromAnim()
    {
        if(weaponHitBox == null) return;
        weaponHitBox.DeActivate();
    }
}
