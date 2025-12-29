using UnityEngine;

public class NormalEnemy : EnemyBase
{
    [Header("애니메이션")]
    [SerializeField] private MonsterAnimation monsterAnim;

    [Header("피격 피드백")]
    [SerializeField] private DamageFeedback damageFeedback;

    [Header("피격 시 Hit 트리거 사용")]
    [SerializeField] private bool playHitOnDamaged = true;

    protected override void Awake()
    {
        base.Awake();

        if (monsterAnim == null)
            monsterAnim = GetComponent<MonsterAnimation>() ?? GetComponentInChildren<MonsterAnimation>(true);

        if (damageFeedback == null)
            damageFeedback = GetComponentInChildren<DamageFeedback>(true);
    }

    protected override void OnDamaged(DamageInfo info)
    {
        base.OnDamaged(info);
        if (IsDead) return;

        damageFeedback?.Play();


        if (playHitOnDamaged)
            monsterAnim?.TryPlayHit();
    }

    protected override void OnDie(DamageInfo info)
    {
        base.OnDie(info);

        // 죽는 애니 트리거
        monsterAnim?.PlayDie();

    }

    // Die 애니메이션 마지막 프레임에 Animation Event로 호출
    public void Anim_DestroySelf()
    {
        GetComponent<TutorialEnemy>()?.OnTutorialEnemyDead();
        Destroy(gameObject);
    }
}
