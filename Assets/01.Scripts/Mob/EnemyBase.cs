using System;
using System.Collections;
using Unity.VisualScripting;
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

    // 이동/상태머신 담당
    protected Transform target;

    [Header("브레이크 시스템")]
    [SerializeField] protected bool useBreakSystem = false;
    [Tooltip("몇 대 맞으면 브레이크(그로기) 되는지")]
    [SerializeField] protected int breakHitThreshold = 10;
    [Tooltip("그로기 유지 시간(초)")]
    [SerializeField] protected float breakGroggyDuration = 5f;
    [Tooltip("그로기 중 추가 피해 배율 (0.2 = 20% 더 아픔)")]
    [Range(0f, 5f)]
    [SerializeField] protected float groggyExtraDamageRatio = 0.2f;

    [Tooltip("그로기 애니메이션 트리거(없으면 비워도 됨)")]
    [SerializeField] protected string breakGroggyTriggerName = "BreakGroggy";
    private static readonly int HashIsGroggy = Animator.StringToHash("IsGroggy");

    public event Action<int, int> OnBreakHitChanged;
    public event Action<bool> OnGroggyChanged;

    protected int breakHitCount = 0;
    protected bool isGroggy = false;

    protected Animator anim;
    private Coroutine groggyCo;
    protected EnemyAIController ai;

    protected override void Awake()
    {
        base.Awake();
        anim = GetComponentInChildren<Animator>();
        ai = GetComponent<EnemyAIController>();
    }

    protected void TryAccumulateBreak()
    {
        if (!useBreakSystem) return;
        if (IsDead) return;
        if (isGroggy) return;
        if (breakHitThreshold <= 0) return;

        breakHitCount++;
        OnBreakHitChanged?.Invoke(breakHitCount, breakHitThreshold);

        if (breakHitCount >= breakHitThreshold)
        {
            if (groggyCo != null) StopCoroutine(groggyCo);
            groggyCo = StartCoroutine(BreakGroggyRoutine());
        }
    }

    private IEnumerator BreakGroggyRoutine()
    {
        if (isGroggy) yield break;

        isGroggy = true;
        OnGroggyChanged?.Invoke(true);

        if (anim != null)
            anim.SetBool(HashIsGroggy, true);

        if (ai != null)
            ai.EnterBreakGroggy(breakGroggyDuration, breakGroggyTriggerName);

        yield return new WaitForSeconds(breakGroggyDuration);

        breakHitCount = 0;
        OnBreakHitChanged?.Invoke(breakHitCount, breakHitThreshold);

        isGroggy = false;
        OnGroggyChanged?.Invoke(false);

        if (anim != null)
            anim.SetBool(HashIsGroggy, false);

        groggyCo = null;
    }

    protected override float GetIncomingDamageMultiplier(DamageInfo info)
    {
        float mul = base.GetIncomingDamageMultiplier(info);
        if (isGroggy) mul *= (1f + groggyExtraDamageRatio);
        return mul;
    }

    protected override void OnDie(DamageInfo info)
    {
        base.OnDie(info);

        if (groggyCo != null)
        {
            StopCoroutine(groggyCo);
            groggyCo = null;
        }

        isGroggy = false;
    }

    protected virtual void OnPhaseChanged(int newPhase) { }

    public int BreakHitCount => breakHitCount;
    public int BreakHitThreshold => breakHitThreshold;
    public bool IsGroggy => isGroggy;
}
