using System.Collections;
using System.Collections.Generic;
using UnityEngine;
    //플레이어 허브로 사용
public class PlayerCharacter : CharacterBase
{
    [Header("캐싱")]
    [SerializeField] private PlayerInputController input;
    [SerializeField] private PhysicsCharacter physicsChar;
    [SerializeField] private PlayerAnimation anim;
    [SerializeField] private PlayerCombat combat;
    [SerializeField] private PlayerParry parry;
    [SerializeField] private WeaponHitBox weaponHitBox;

    protected override void Awake()
    {
        base.Awake();
        input = GetComponent<PlayerInputController>();
        physicsChar = GetComponent<PhysicsCharacter>();
        anim = GetComponent<PlayerAnimation>();
        combat = GetComponent<PlayerCombat>();
        parry = GetComponent <PlayerParry>();
        weaponHitBox = GetComponentInChildren<WeaponHitBox>();
    }

    protected override float GetIncomingDamageMultiplier(DamageInfo info)
    {
        if (parry != null && parry.IsParryGuardActive)
            return 0f;

        return 1f;
    }

    protected override void OnDie(DamageInfo info)
    {
        // 여기서 입력 락, 애니, 게임오버 처리
        input?.Lock();
        physicsChar.SetMovementLocked(true);
        anim?.PlayDie(); // 함수명은 네꺼에 맞춰
    }
}
