using UnityEngine;

public class PlayerCombat : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private PlayerAnimation playerAnim;
    [SerializeField] private PhysicsCharacter physicsCharacter;
    [SerializeField] private PlayerInputController playerInput;
    [SerializeField] private WeaponHitBox weaponHitBox;
    [SerializeField] private InventoryManager inventoryManager;

    [Header("콤보 설정")]
    [SerializeField] private int maxCombo = 3;

    [Header("기본 공격력 (스탯/무기 공격력")]
    [SerializeField] private float baseDamage = 10f;

    [Header("콤보별 데미지")]
    [SerializeField] private float[] comboDamages = { 1.0f, 1.2f, 1.5f, 1.8f, 2.0f };

    [Header("공격 전진 힘")]
    [SerializeField] private float[] forwardPowers = { 1f, 0.5f, 1f };

    [Header("공격 후퇴 힘")]
    [SerializeField] private float[] backwardPowers = { 0f, 0f, 0f, 0f, 0f };

    [Header("무기 상태")]
    [SerializeField] private bool weaponEquipped = false;
    public bool WeaponEquipped => weaponEquipped;

    [Header("공격 상태")]
    [SerializeField] private bool isAttacking = false;
    public bool IsAttacking => isAttacking;

    public bool IsDashing => physicsCharacter != null && physicsCharacter.IsDashing;

    [Header("대쉬 무적 시간")]
    [SerializeField] private float dashInvincibleExtra = 0.05f;

    private PlayerParry parry;

    int currentCombo = 0;
    bool bufferedNextInput = false;
    float lastAttackTime = 0f;

    void Awake()
    {
        if (playerAnim == null)
            playerAnim = GetComponentInChildren<PlayerAnimation>();
        if (physicsCharacter == null)
            physicsCharacter = GetComponent<PhysicsCharacter>();
        if (playerInput == null)
            playerInput = GetComponent<PlayerInputController>();
        if (weaponHitBox == null)
            weaponHitBox = GetComponentInChildren<WeaponHitBox>();
        if (inventoryManager == null)
            inventoryManager = InventoryManager.Instance;

        parry = GetComponent<PlayerParry>();
    }

    public bool TryDash(Vector3 dir)
    {
        if (isAttacking)
            CancelAttackCommon();

        bool started = physicsCharacter.TryDash(dir);
        if (!started) return false;

        var character = GetComponent<CharacterBase>();
        if (character != null)
            character.StartInvincible(physicsCharacter.dashDuration + dashInvincibleExtra);

        playerAnim?.PlayDash();
        return true;
    }

    public void OnAttackInput()
    {
        if (!weaponEquipped) return;
        if (playerInput != null && playerInput.isLocked) return;
        if (IsDashing) return;

        if (parry != null && parry.isParryStance) return;

        if (!isAttacking)
        {
            StartFirstAttack();
            return;
        }

        if (bufferedNextInput) return;
        bufferedNextInput = true;
    }

    //  패링 입력 들어오면 “자세/락/애니”만
    public void TryStartParryStance()
    {
        if (IsAttacking) return;
        if (!weaponEquipped) return;
        if (IsDashing) return;
        if (parry != null && parry.isParryStance) return;

        parry?.EnterStance();

        physicsCharacter?.SetMovementLocked(true);
        ClearAttackBuffer();

        playerAnim?.PlayParry(); // 패링 애니 재생
    }

    //  성공 연출만 (몬스터 OnParried는 절대 건드리지 마)
    public void OnParrySuccess(Transform attacker, Vector3 hitPoint)
    {
        // 여기서 성공 이펙트/사운드/카메라/짧은 무적 등만 처리
        // Debug.Log("[Parry] SUCCESS");

        // 예시) 잠깐 무적
        var character = GetComponent<CharacterBase>();
        if (character != null)
            character.StartInvincible(0.15f);

        // 성공 애니가 따로 있으면
        // playerAnim?.PlayParrySuccess();
    }

    void ClearAttackBuffer() => bufferedNextInput = false;

    public void OnToggleWeaponInput()
    {
        if (inventoryManager == null)
        {
            Debug.LogWarning("[PlayerCombat] inventoryManager 가 없습니다.");
            return;
        }

        var equippedWeapon = inventoryManager.GetEquippedWeapon();
        if (equippedWeapon == null)
        {
            Debug.Log("[PlayerCombat] 장착된 무기가 없어 무기를 꺼낼 수 없습니다.");
            return;
        }
        weaponEquipped = !weaponEquipped;
        playerAnim?.SetWeaponEquipped(weaponEquipped);

        physicsCharacter.SetWeaponEquipped(weaponEquipped);

        if (!weaponEquipped && isAttacking)
            ForceStopAttack();
    }


    void StartFirstAttack()
    {
        isAttacking = true;
        bufferedNextInput = false;

        currentCombo = 1;
        lastAttackTime = Time.time;

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

        physicsCharacter.SetMovementLocked(true);
        playerAnim?.Attack(currentCombo);
    }

    void ResetCombo()
    {
        isAttacking = false;
        bufferedNextInput = false;
        currentCombo = 0;

        physicsCharacter.SetMovementLocked(false);

        playerAnim?.SetIsAttacking(false);
        playerAnim?.PlayIdle();
    }

    void ForceStopAttack() => ResetCombo();

    void CancelAttackCommon()
    {
        weaponHitBox?.DeActivate();

        isAttacking = false;
        bufferedNextInput = false;
        currentCombo = 0;

        physicsCharacter.SetMovementLocked(false);
        playerAnim?.SetIsAttacking(false);
    }

    public void CancelAttackForDash()
    {
        if (!isAttacking) return;
        CancelAttackCommon();
    }

    //======== 애니메이션 이벤트 ========
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

    public void OnAttackHitStartFromAnim()
    {
        if (weaponHitBox == null) return;
        if (currentCombo <= 0) return;

        int idx = Mathf.Clamp(currentCombo - 1, 0, comboDamages.Length - 1);
        float multiplier = comboDamages[idx];
        float finalDamage = baseDamage * multiplier;

        weaponHitBox.Activate(finalDamage);
    }

    public void OnAttackHitEndFromAnim()
    {
        if (weaponHitBox == null) return;
        weaponHitBox.DeActivate();
    }

    //  패링 모션 끝 이벤트(자세 해제 + 락 해제)
    public void EvParryEnd()
    {
        parry?.ExitStance();
        physicsCharacter?.SetMovementLocked(false);
    }
}
