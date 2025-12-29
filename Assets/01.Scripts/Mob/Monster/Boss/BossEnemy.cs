using System;
using System.Collections;
using UnityEngine;

public class BossEnemy : EnemyBase
{
    [Header("보스 페이즈 설정")]
    public int maxPhase = 1;
    public float phase2HpRatio = 0.5f;
    public float phase3HpRatio = 0.2f;

    [Header("페이즈별 이동 속도 배율")]
    private float baseMoveSpeed;
    public float phase2MoveSpeedMultiplier = 1.2f;
    public float phase3MoveSpeedMultiplier = 1.4f;

    [Header("코어 오브젝트 설정")]
    [SerializeField] private GameObject coreObject;

    [Header("피격 연출")]
    [SerializeField] private DamageFeedback damageFeedback;

    [Header("UI 참조")]
    [SerializeField] private BossUIStatus bossUI;

    [Header("브레이크 시스템")]
    public int breakEnableFromPhase = 2;
    public int breakHitThreshold = 10;
    public float breakGroggyDuration = 5f;
    [Range(0f, 5f)] public float groggyExtraDamageRatio = 0.2f;
    public string breakGroggyTriggerName = "BreakGroggy";

    [Header("튜토보스 3페이즈 강제 종료")]
    [SerializeField] private bool isTutorialBoss = false;
    [SerializeField] private string phase3FinaleTriggerName = "Phase3Finale";

    [Header("3페이즈 연출 길게 (3Phase_2 루프 후 3Phase_3로)")]
    [Tooltip("3Phase_2를 몇 초 동안 반복할지(실시간). 0이면 즉시 트리거.")]
    [SerializeField] private float phase3Phase2LoopSeconds = 3f;

    [Tooltip("3Phase_2 -> 3Phase_3로 넘어갈 때 사용할 트리거")]
    [SerializeField] private string phase3Phase2To3TriggerName = "Phase3Finale_To3";

    [Tooltip("Animator 상태 이름(정확히 일치해야 함)")]
    [SerializeField] private string phase3State2Name = "3Phase_2";

    [Tooltip("Animator 레이어 인덱스(보통 0)")]
    [SerializeField] private int animatorLayerIndex = 0;

    public event Action<int, int> OnBreakHitChanged;
    public event Action<bool> OnGroggyChanged;

    private int breakHitCount = 0;
    private bool isGroggy = false;

    private BossAIController ai;
    private MonsterAnimation monsterAnim;
    private EnemyMeleeAttack meleeAttack;
    private Animator anim;

    public int CurrentPhase { get; private set; } = 1;

    private bool phase3FinaleStarted = false;
    private bool phase3FinaleKillDone = false;

    private Coroutine phase3FinaleCo;

    protected override void Awake()
    {
        base.Awake();

        baseMoveSpeed = moveSpeed;
        ai = GetComponent<BossAIController>();
        monsterAnim = GetComponent<MonsterAnimation>();
        meleeAttack = GetComponent<EnemyMeleeAttack>();
        anim = GetComponentInChildren<Animator>(); // 캐싱

        if (damageFeedback == null)
            damageFeedback = GetComponentInChildren<DamageFeedback>(true);

        if (bossUI == null)
            bossUI = FindObjectOfType<BossUIStatus>();

        if (bossUI != null)
            bossUI.SetBoss(this);

        if (coreObject != null)
            coreObject.SetActive(false);

        // 보스 attackRange 동기화(원하면 유지)
        if (meleeAttack != null)
            attackRange = meleeAttack.hitRadius;
    }

    protected override float GetIncomingDamageMultiplier(DamageInfo info)
    {
        float mul = 1f;
        if (isGroggy) mul *= (1f + groggyExtraDamageRatio);
        return mul;
    }

    protected override void OnDamaged(DamageInfo info)
    {
        base.OnDamaged(info);
        if (IsDead) return;

        float hpRatio = currentHp / maxHp;

        bossUI?.UpdateHp(currentHp, maxHp);
        damageFeedback?.Play();

        TryAccumulateBreak();

        if (CurrentPhase == 1 && maxPhase >= 2 && hpRatio <= phase2HpRatio)
        {
            EnterPhase(2);
        }
        else if (CurrentPhase == 2 && maxPhase >= 3 && hpRatio <= phase3HpRatio)
        {
            EnterPhase(3);
        }

        // 튜토보스 3페이즈 강제 연출 시작
        if (CurrentPhase == 3 && isTutorialBoss && !phase3FinaleStarted)
        {
            phase3FinaleStarted = true;

            // 보스 더 이상 안 맞게(무적 유지)
            StartInvincible(999999f);

            // 전용 트리거 발동 -> 3Phase_1 진입
            if (anim != null && !string.IsNullOrEmpty(phase3FinaleTriggerName))
            {
                anim.ResetTrigger(phase3FinaleTriggerName);
                anim.SetTrigger(phase3FinaleTriggerName);
            }

            // AI 멈춤 유지
            if (ai != null) ai.enabled = false;

            // 3Phase_2를 일정 시간 루프 후 3Phase_3 트리거 발동
            if (phase3FinaleCo != null) StopCoroutine(phase3FinaleCo);
            phase3FinaleCo = StartCoroutine(Phase3Finale_ToPhase3Routine());
        }
    }

    private IEnumerator Phase3Finale_ToPhase3Routine()
    {
        if (anim == null) yield break;

        // 1 3Phase_2 상태에 들어갈 때까지 대기
        float timeout = 10f;
        while (timeout > 0f && !IsInStateOrNext(phase3State2Name))
        {
            timeout -= Time.unscaledDeltaTime;
            yield return null;
        }

        // 2 설정 시간만큼 3Phase_2 루프 유지
        float wait = Mathf.Max(0f, phase3Phase2LoopSeconds);
        if (wait > 0f)
            yield return new WaitForSecondsRealtime(wait);

        // 3 3Phase_3로 넘기는 트리거 발동
        if (!string.IsNullOrEmpty(phase3Phase2To3TriggerName))
        {
            anim.ResetTrigger(phase3Phase2To3TriggerName);
            anim.SetTrigger(phase3Phase2To3TriggerName);
        }

        phase3FinaleCo = null;
    }

    private bool IsInStateOrNext(string stateName)
    {
        if (anim == null || string.IsNullOrEmpty(stateName)) return false;

        var cur = anim.GetCurrentAnimatorStateInfo(animatorLayerIndex);
        if (cur.IsName(stateName)) return true;

        if (anim.IsInTransition(animatorLayerIndex))
        {
            var next = anim.GetNextAnimatorStateInfo(animatorLayerIndex);
            if (next.IsName(stateName)) return true;
        }

        return false;
    }

    private void EnterPhase(int newPhase)
    {
        if (newPhase <= CurrentPhase) return;

        CurrentPhase = Mathf.Clamp(newPhase, 1, maxPhase);
        ApplyPhaseStats();

        if (CurrentPhase >= 2 && coreObject != null)
            coreObject.SetActive(true);

        if (bossUI != null)
            bossUI.SetBreakVisible(CurrentPhase >= breakEnableFromPhase);
    }

    private void ApplyPhaseStats()
    {
        switch (CurrentPhase)
        {
            case 1: moveSpeed = baseMoveSpeed; break;
            case 2: moveSpeed = baseMoveSpeed * phase2MoveSpeedMultiplier; break;
            case 3: moveSpeed = baseMoveSpeed * phase3MoveSpeedMultiplier; break;
            default: moveSpeed = baseMoveSpeed; break;
        }
    }

    private void TryAccumulateBreak()
    {
        if (isGroggy) return;
        if (CurrentPhase < breakEnableFromPhase) return;
        if (breakHitThreshold <= 0) return;

        breakHitCount++;
        OnBreakHitChanged?.Invoke(breakHitCount, breakHitThreshold);
        bossUI?.UpdateBreak(breakHitCount, breakHitThreshold);

        if (breakHitCount >= breakHitThreshold)
            StartCoroutine(BreakGroggyRoutine());
    }

    private IEnumerator BreakGroggyRoutine()
    {
        if (isGroggy) yield break;

        isGroggy = true;
        OnGroggyChanged?.Invoke(true);
        bossUI?.SetGroggy(true);

        if (ai != null)
            ai.EnterBreakGroggy(breakGroggyDuration, breakGroggyTriggerName);

        yield return new WaitForSeconds(breakGroggyDuration);

        breakHitCount = 0;
        OnBreakHitChanged?.Invoke(breakHitCount, breakHitThreshold);
        bossUI?.UpdateBreak(breakHitCount, breakHitThreshold);

        isGroggy = false;
        OnGroggyChanged?.Invoke(false);
        bossUI?.SetGroggy(false);
    }

    protected override void OnDie(DamageInfo info)
    {
        base.OnDie(info);

        if (phase3FinaleCo != null)
        {
            StopCoroutine(phase3FinaleCo);
            phase3FinaleCo = null;
        }

        if (coreObject != null)
            coreObject.SetActive(false);

        if (bossUI != null)
            bossUI.SetVisible(false);

        monsterAnim?.PlayDie();
    }

    public void Anim_DestroySelf() => Destroy(gameObject);

    public void Anim_Phase3Finale_KillPlayer()
    {
        Debug.Log("[BossEnemy] Anim_Phase3Finale_KillPlayer CALLED", this);

        if (!phase3FinaleStarted) return;
        if (phase3FinaleKillDone) return;
        phase3FinaleKillDone = true;

        var playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj == null) return;

        var player = playerObj.GetComponentInParent<CharacterBase>();
        if (player == null) return;

        var info = new DamageInfo(
            amount: 999999f,
            point: player.transform.position,
            normal: Vector3.up,
            reason: DamageReason.TutorialBossPhase3Finale
        );

        player.TakeDamage(info);
    }
}
