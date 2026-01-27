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
    public int bossBreakHitThreshold = 10;
    public float bossBreakGroggyDuration = 5f;
    [Range(0f, 5f)] public float bossGroggyExtraDamageRatio = 0.2f;
    public string bossBreakGroggyTriggerName = "BreakGroggy";
    private MonsterParryHandler _parryHandler;

    [Header("튜토보스 3페이즈 강제 종료")]
    [SerializeField] private bool isTutorialBoss = false;
    [SerializeField] private string phase3FinaleTriggerName = "Phase3Finale";

    [Header("튜토보스 사망 연출 후 정리")]
    [SerializeField] private float tutorialBossDestroyDelayAfterKill = 3f;
    private Coroutine tutorialBossDestroyCo;

    [Header("3페이즈 연출 길게")]
    [SerializeField] private float phase3Phase2LoopSeconds = 3f;
    [SerializeField] private string phase3Phase2To3TriggerName = "Phase3Finale_To3";
    [SerializeField] private string phase3State2Name = "3Phase_2";
    [SerializeField] private int animatorLayerIndex = 0;

    [Header("퀘스트 / 데이터 ID")]
    [SerializeField] private int monsterId;

    [SerializeField] private float tutorialBossDisableDelayAfterKill = 3f;
    private Coroutine tutorialBossDisableCo;

    public event Action<int, int> bossOnBreakHitChanged;
    public event Action<bool> BossOnGroggyChanged;

    private BossAIController ai;
    private MonsterAnimation monsterAnim;
    private EnemyMeleeAttack meleeAttack;

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
        anim = GetComponentInChildren<Animator>();

        if (damageFeedback == null)
            damageFeedback = GetComponentInChildren<DamageFeedback>(true);

        if (bossUI == null)
            bossUI = FindObjectOfType<BossUIStatus>();

        if (coreObject != null)
            coreObject.SetActive(false);

        // 보스 attackRange 동기화
        if (meleeAttack != null)
            attackRange = meleeAttack.hitRadius;

        _parryHandler = GetComponentInChildren<MonsterParryHandler>(true);
    }

    public void InitializeForSession(Transform player)
    {
        target = player; // EnemyBase의 target 사용
        ai?.ResetForReuse(player);

        if (bossUI == null) bossUI = BossUIStatus.Instance;
        if (bossUI != null)
        {
            bossUI.SetBoss(this);
            bossUI.SetVisible(true);
            bossUI.UpdateHp(currentHp, maxHp);
            bossUI.SetGroggy(false);
            bossUI.SetBreakVisible(CurrentPhase >= breakEnableFromPhase);
        }
    }

    protected override float GetIncomingDamageMultiplier(DamageInfo info)
    {
        float mul = 1f;
        if (isGroggy) mul *= (1f + bossGroggyExtraDamageRatio);
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

        if (CurrentPhase == 1 && maxPhase >= 2 && hpRatio <= phase2HpRatio) EnterPhase(2);
        else if (CurrentPhase == 2 && maxPhase >= 3 && hpRatio <= phase3HpRatio) EnterPhase(3);

        // 튜토보스 3페이즈 강제 연출
        if (CurrentPhase == 3 && isTutorialBoss && !phase3FinaleStarted)
        {
            phase3FinaleStarted = true;

            StartInvincible(999999f);

            if (anim != null && !string.IsNullOrEmpty(phase3FinaleTriggerName))
            {
                anim.ResetTrigger(phase3FinaleTriggerName);
                anim.SetTrigger(phase3FinaleTriggerName);
            }

            if (ai != null) ai.enabled = false;

            if (phase3FinaleCo != null) StopCoroutine(phase3FinaleCo);
            phase3FinaleCo = StartCoroutine(Phase3Finale_ToPhase3Routine());
        }
    }

    private IEnumerator Phase3Finale_ToPhase3Routine()
    {
        if (anim == null) yield break;

        float timeout = 10f;
        while (timeout > 0f && !IsInStateOrNext(phase3State2Name))
        {
            timeout -= Time.unscaledDeltaTime;
            yield return null;
        }

        float wait = Mathf.Max(0f, phase3Phase2LoopSeconds);
        if (wait > 0f) yield return new WaitForSecondsRealtime(wait);

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

        bossUI?.SetBreakVisible(CurrentPhase >= breakEnableFromPhase);
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
        if (bossBreakHitThreshold <= 0) return;

        breakHitCount++;
        bossOnBreakHitChanged?.Invoke(breakHitCount, bossBreakHitThreshold);
        bossUI?.UpdateBreak(breakHitCount, bossBreakHitThreshold);

        if (breakHitCount >= bossBreakHitThreshold)
            StartCoroutine(BreakGroggyRoutine());
    }

    private IEnumerator BreakGroggyRoutine()
    {
        if (isGroggy) yield break;

        if (_parryHandler == null)
            _parryHandler = GetComponentInChildren<MonsterParryHandler>(true);

        _parryHandler?.ForceCancelParryTelegraph();

        isGroggy = true;
        BossOnGroggyChanged?.Invoke(true);
        bossUI?.SetGroggy(true);

        ai?.EnterBreakGroggy(bossBreakGroggyDuration, bossBreakGroggyTriggerName);

        yield return new WaitForSeconds(bossBreakGroggyDuration);

        breakHitCount = 0;
        bossOnBreakHitChanged?.Invoke(breakHitCount, bossBreakHitThreshold);
        bossUI?.UpdateBreak(breakHitCount, bossBreakHitThreshold);

        isGroggy = false;
        BossOnGroggyChanged?.Invoke(false);
        bossUI?.SetGroggy(false);
    }

    public override void OnDie(DamageInfo info)
    {
        base.OnDie(info);

        if (!isTutorialBoss)
            MonsterKillEvent.Raise(monsterId);

        if (phase3FinaleCo != null)
        {
            StopCoroutine(phase3FinaleCo);
            phase3FinaleCo = null;
        }

        if (coreObject != null)
            coreObject.SetActive(false);

        bossUI?.SetVisible(false);

        monsterAnim?.PlayDie();
    }

    public void Anim_DestroySelf() => Destroy(gameObject);

    public void ResetForRetry()
    {
        if (phase3FinaleCo != null) { StopCoroutine(phase3FinaleCo); phase3FinaleCo = null; }
        if (tutorialBossDestroyCo != null) { CoroutineRunner.Stop(tutorialBossDestroyCo); tutorialBossDestroyCo = null; }

        phase3FinaleStarted = false;
        phase3FinaleKillDone = false;

        ResetCharacter();

        if (baseMoveSpeed <= 0.0001f)
            baseMoveSpeed = Mathf.Max(0.0001f, moveSpeed);

        CurrentPhase = 1;
        moveSpeed = baseMoveSpeed;

        breakHitCount = 0;
        isGroggy = false;
        bossOnBreakHitChanged?.Invoke(breakHitCount, bossBreakHitThreshold);
        BossOnGroggyChanged?.Invoke(false);

        if (coreObject != null)
            coreObject.SetActive(false);

        if (anim != null)
        {
            anim.Rebind();
            anim.Update(0f);
        }

        // AI도 깨끗하게 리셋
        ai?.ResetForReuse(null);

        if (bossUI == null) bossUI = BossUIStatus.Instance;
        if (bossUI != null)
        {
            bossUI.SetBoss(this);
            bossUI.SetVisible(true);
            bossUI.UpdateHp(currentHp, maxHp);
            bossUI.SetGroggy(false);
            bossUI.UpdateBreak(breakHitCount, bossBreakHitThreshold);
            bossUI.SetBreakVisible(CurrentPhase >= breakEnableFromPhase);
        }
    }

    public void Anim_Phase3Finale_KillPlayer()
    {
        if (!isTutorialBoss) return;
        if (!phase3FinaleStarted) return;
        if (phase3FinaleKillDone) return;
        phase3FinaleKillDone = true;

        if (target == null) return;

        var player = target.GetComponentInParent<CharacterBase>();
        if (player == null) return;

        var info = new DamageInfo(
            amount: 999999f,
            point: player.transform.position,
            normal: Vector3.up,
            reason: DamageReason.TutorialBossPhase3Finale
        );

        player.ForceKill(info);

    }
}
