using UnityEngine;

public enum EnemyRank
{
    Normal,
    Elite,
    Boss
}

public abstract class EnemyBase : CharacterBase
{
    [Header("적 정보")]
    [Tooltip("플레이어를 감지하는 거리")]
    public float detectRange = 10f;

    [Tooltip("몬스터가 공격을 시도하는 기본 거리")]
    public float attackRange = 2f;

    public bool IsDead => !IsAlive;

    protected Transform target;
    // 이동/상태머신 담당

    protected override void Awake()
    {
        base.Awake();
    }

    protected override void OnDie(DamageInfo info)
    {
        base.OnDie(info);
    }
}
