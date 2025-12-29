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

    public event Action<int, int> OnBreakHitChanged;
    public event Action<bool> OnGroggyChanged;

    private int breakHitCount = 0;
    private bool isGroggy = false;

    private BossAIController ai;
    private MonsterAnimation monsterAnim;
    private EnemyMeleeAttack meleeAttack;

    public int CurrentPhase { get; private set; } = 1;

    private bool phase3FinaleStarted = false;
    private bool phase3FinaleKillDone = false;

    protected override void Awake()
    {
        base.Awake();

        baseMoveSpeed = moveSpeed;
        ai = GetComponent<BossAIController>();
        monsterAnim = GetComponent<MonsterAnimation>();
        meleeAttack = GetComponent<EnemyMeleeAttack>(); // ✅

        if (damageFeedback == null)
            damageFeedback = GetComponentInChildren<DamageFeedback>(true);

        if (bossUI == null)
            bossUI = FindObjectOfType<BossUIStatus>();

        if (bossUI != null)
            bossUI.SetBoss(this);

        if (coreObject != null)
            coreObject.SetActive(false);

        // ✅ “보스 attackRange 값”도 hitRadius랑 통일(동기화)
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

        if (CurrentPhase == 3 && isTutorialBoss && !phase3FinaleStarted)
        {
            phase3FinaleStarted = true;

            StartInvincible(999999f);

            var anim = GetComponentInChildren<Animator>();
            if (anim != null && !string.IsNullOrEmpty(phase3FinaleTriggerName))
            {
                anim.ResetTrigger(phase3FinaleTriggerName);
                anim.SetTrigger(phase3FinaleTriggerName);
            }

            if (ai != null) ai.enabled = false;
        }
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
