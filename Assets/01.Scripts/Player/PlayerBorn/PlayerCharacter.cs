using UnityEngine;

public class PlayerCharacter : CharacterBase
{
    [Header("캐싱")]
    [SerializeField] private PlayerInputController input;
    [SerializeField] private PhysicsCharacter physicsChar;
    [SerializeField] private PlayerAnimation anim;
    [SerializeField] private PlayerCombat combat;
    [SerializeField] private PlayerParry parry;


    protected override void Awake()
    {
        base.Awake();
        input = GetComponent<PlayerInputController>();
        physicsChar = GetComponent<PhysicsCharacter>();
        anim = GetComponent<PlayerAnimation>();
        combat = GetComponent<PlayerCombat>();
        parry = GetComponent<PlayerParry>();
    }

    protected override float GetIncomingDamageMultiplier(DamageInfo info)
    {
        if (parry != null && parry.IsParryGuardActive)
            return 0f;
        return 1f;
    }


    protected override void OnDie(DamageInfo info)
    {
        // 입력 / 이동 락, 죽음 애니메이션
        input?.Lock();
        if (physicsChar != null) physicsChar.SetMovementLocked(true);
        anim?.PlayDie();

        // 게임오버 UI 호출
        if (GameOverUI.Instance != null)
            GameOverUI.Instance.ShowDeath("YOU DIED");
        else
            Debug.LogWarning("[PlayerCharacter] GameOverUI.Instance가 없음");
    }

    public void RespawnAt(Vector3 position)
    {
        // 위치 이동
        transform.position = position;

        // HP 풀피
        RestoreFullHp(notify: true);

        // 다시 조작 가능
        input?.Unlock();
        if (physicsChar != null) physicsChar.SetMovementLocked(false);

    }
}
