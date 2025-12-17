using UnityEngine;

public class PlayerCombat : MonoBehaviour, IParryReceiver
{
    [Header("참조")]
    [SerializeField] private PlayerAnimation playerAnim;
    [SerializeField] private PhysicsCharacter physicsCharacter;
    [SerializeField] private PlayerInputController playerInput;
    [SerializeField] private WeaponHitBox weaponHitBox;

    [Header("콤보 설정")]
    [SerializeField] private int maxCombo = 3;
    //[SerializeField] private float comboInputWindow = 0.4f;

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
    [SerializeField] bool isAttacking = false;
    public bool IsAttacking => isAttacking;

    public bool IsParrying { get; private set; }
    public bool ParryWindowOpen {  get; private set; }

    int currentCombo = 0;
    bool bufferedNextInput = false;
    float lastAttackTime = 0f;


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

    public bool TryDash(Vector3 dir)
    {
        if (isAttacking)
            CancelAttackCommon();

        physicsCharacter.TryDash(dir);
        playerAnim?.PlayDash();
        return true;
    }
    public void OnAttackInput()
    {
        if (!weaponEquipped) return;   // 무기 안 들면 공격 안됨

        if(IsParrying) return;

        if(playerInput.isLocked) return;

        if (physicsCharacter.IsDashing) return;

        if (!isAttacking)
        {
            StartFirstAttack();
            return;
        }

        if (bufferedNextInput) return;

        //if (Time.time - lastAttackTime <= comboInputWindow)
            bufferedNextInput = true;
    }

    public void TryStartParry()
    {
        if (IsAttacking) return;          // 공격 중 패링 금지(원하면 캔슬로 바꿀 수 있음)
        if (!weaponEquipped) return;
        if (physicsCharacter.IsDashing) return;
        if (IsParrying) return;

        IsParrying = true;
        physicsCharacter?.SetMovementLocked(true);
        ClearAttackBuffer();              // 패링 중 좌클릭이 남지 않게

        playerAnim.PlayParry();
    }

    // WeaponHitBox가 호출
    public bool TryParry(WeaponHitBox hitBox, Vector3 hitPoint)
    {
        if (!IsParrying || !ParryWindowOpen) return false;

        // 공격자 방향(몬스터)이 없으면 그냥 패링 허용/거부 중 하나
        Transform attacker = hitBox != null ? hitBox.OwnerRoot : null;
        if (attacker == null) return false; // 보수적으로: 공격자 없으면 패링 실패

        //  정면 판정: 내 forward 기준으로 공격자가 앞쪽에 있어야 함
        Vector3 toAttacker = attacker.position - transform.position;
        toAttacker.y = 0f;

        Vector3 fwd = transform.forward;
        fwd.y = 0f;

        if (toAttacker.sqrMagnitude < 0.0001f) return true; // 거의 겹치면 일단 성공 처리

        toAttacker.Normalize();
        fwd.Normalize();

        // dot: 1이면 정면, 0이면 옆, -1이면 뒤
        float dot = Vector3.Dot(fwd, toAttacker);

        // 0.5  = 약 60도 안쪽만 허용
        // 0.2  = 약 78도 안쪽 허용
        const float MIN_FACING_DOT = 0.3f; // 너 게임 느낌대로 0.2~0.5에서 조절

        if (dot < MIN_FACING_DOT)
            return false; // 옆/뒤에서 들어온 공격은 패링 실패
        // 성공
        ParryWindowOpen = false; // 한 번 성공하면 닫기(연타 무료 방지)
        return true;
    }

    void ClearAttackBuffer()
    {
        bufferedNextInput = false;
    }


    public void OnToggleWeaponInput()
    {
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
        playerAnim.PlayIdle();

    }

    void ForceStopAttack()
    {
        ResetCombo();
    }

    void CancelAttackCommon()   //대쉬 중 공격 관한것들 캔슬
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

    public void EvParryWindowOpen()
    {
        ParryWindowOpen = true;
    }

    public void EvParryWindowClose()
    {
        ParryWindowOpen = false;
    }

    public void EvParryEnd()
    {
        ParryWindowOpen = false;
        IsParrying = false;
        physicsCharacter?.SetMovementLocked(false);
    }
}
