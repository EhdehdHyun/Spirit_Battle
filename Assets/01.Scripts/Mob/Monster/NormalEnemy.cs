using UnityEngine;

public class NormalEnemy : EnemyBase, IAoeDamageable
{
    [Header("애니메이션")]
    [SerializeField] private MonsterAnimation monsterAnim;

    [Header("피격 피드백")]
    [SerializeField] private DamageFeedback damageFeedback;

    [Header("피격 시 Hit 트리거 사용")]
    [SerializeField] private bool playHitOnDamaged = true;

    public int monsterId = 10000; //몬스터 ID

    protected override void Awake()
    {
        base.Awake();

        if (monsterAnim == null)
            monsterAnim = GetComponent<MonsterAnimation>() ?? GetComponentInChildren<MonsterAnimation>(true);

        if (damageFeedback == null)
            damageFeedback = GetComponentInChildren<DamageFeedback>(true);
    }
    private void OnEnable()
    {
        Debug.Log($"[Enemy] OnEnable {name} IsDead={IsDead}");
        if (monsterId > 0 && QuestTargetRegistry.Instance != null)
        {
            QuestTargetRegistry.Instance.Register(monsterId, transform);
        }
    }

    protected override void OnDamaged(DamageInfo info)
    {
        base.OnDamaged(info);
        if (IsDead) return;
        
        damageFeedback?.Play();

        TryAccumulateBreak();

        if (playHitOnDamaged)
            monsterAnim?.TryPlayHit();
    }

    protected override void OnDie(DamageInfo info)
    {
        base.OnDie(info);

        // 죽는 애니 트리거
        monsterAnim?.PlayDie();
        
        if (monsterId > 0)
        {
            QuestManager.Instance.OnMonsterKilled(monsterId);
            QuestTargetRegistry.Instance?.Unregister(monsterId, transform);
            Debug.Log($"[Registry] Unregister monsterId={monsterId} name={name}");
        }
    }

    // Die 애니메이션 마지막 프레임에 Animation Event로 호출
    public void Anim_DestroySelf()
    {
        GetComponent<TutorialEnemy>()?.OnTutorialEnemyDead();
        Destroy(gameObject);
    }

}
