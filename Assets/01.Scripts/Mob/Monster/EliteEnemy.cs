using UnityEngine;

public class EliteEnemy : EnemyBase
{
    [Header("애니메이션")]
    [SerializeField] private MonsterAnimation monsterAnim;

    [Header("피격 시 Hit 트리거 사용")]
    [SerializeField] private bool playHitOnDamaged = true;

    protected override void Awake()
    {
        base.Awake();

        if (monsterAnim == null)
            monsterAnim = GetComponent<MonsterAnimation>() ?? GetComponentInChildren<MonsterAnimation>(true);
    }

    protected override void OnDamaged(DamageInfo info)
    {
        base.OnDamaged(info);
        if (IsDead) return;

        if (playHitOnDamaged)
            monsterAnim?.TryPlayHit();
    }

    protected override void OnDie(DamageInfo info)
    {
        base.OnDie(info);
        monsterAnim?.PlayDie();
    }

    public void Anim_DestroySelf()
    {
        Destroy(gameObject);
    }
}
