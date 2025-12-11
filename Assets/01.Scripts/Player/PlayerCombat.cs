using UnityEngine;

public class PlayerCombat : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private PlayerAnimation playerAnim;
    [SerializeField] private PhysicsCharacter physicsCharacter;

    [Header("콤보 설정")]
    [SerializeField] private int maxCombo = 3;
    [SerializeField] private float comboInputWindow = 0.4f;

    [Header("공격 전진 힘")]
    [SerializeField] private float[] forwardPowers = { 2f, 2.5f, 3f };

    [Header("공격 후퇴 힘")]
    [SerializeField] private float[] backwardPowers = { 1f, 1.2f, 1.5f };

    [Header("무기 상태")]
    [SerializeField] private bool weaponEquipped = false;
    public bool WeaponEquipped => weaponEquipped;

    int currentCombo = 0;
    bool isAttacking = false;
    bool bufferedNextInput = false;
    float lastAttackTime = 0f;

    void Awake()
    {
        if (playerAnim == null)
            playerAnim = GetComponentInChildren<PlayerAnimation>();
        if (physicsCharacter == null)
            physicsCharacter = GetComponent<PhysicsCharacter>();
    }


    public void OnAttackInput()
    {
        if (!weaponEquipped) return;   // 무기 안 들면 공격 안됨

        if (!isAttacking)
        {
            StartFirstAttack();
            return;
        }

        if (Time.time - lastAttackTime <= comboInputWindow)
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

        playerAnim?.Attack(currentCombo);
    }

    void ResetCombo()
    {
        isAttacking = false;
        bufferedNextInput = false;
        currentCombo = 0;

        playerAnim?.SetIsAttacking(false);
    }

    void ForceStopAttack()
    {
        ResetCombo();
        //여기서 Animator를 강제 Idle로 넘기는 것도 가능
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
        if (physicsCharacter == null) return;
        if (currentCombo <= 0 || currentCombo > forwardPowers.Length) return;

        float power = forwardPowers[currentCombo - 1];
        if (power <= 0f) return;

        Vector3 dir = transform.forward;
        dir.y = 0f;
        dir.Normalize();

        physicsCharacter.AddImpulse(dir * power);
    }

    public void OnAttackStepBackFromAnim()
    {
        if (physicsCharacter == null) return;
        if (currentCombo <= 0 || currentCombo > backwardPowers.Length) return;

        float power = backwardPowers[currentCombo - 1];
        if (power <= 0f) return;

        Vector3 dir = -transform.forward;
        dir.y = 0f;
        dir.Normalize();

        physicsCharacter.AddImpulse(dir * power);
    }
}
