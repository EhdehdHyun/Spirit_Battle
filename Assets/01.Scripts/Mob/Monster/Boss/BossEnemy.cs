using System;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

public class BossEnemy : EnemyBase
{
    [Header("보스 페이즈 설정")]
    [Tooltip("보스의 최대 페이즈 수 (ex. 1, 2, 3)")]
    public int maxPhase = 1;

    [Tooltip("2 페이즈로 넘어가는 HP 비율 (0~1 ex.0.5 = 체력 절반 이하에서 2페이즈")]
    public float phase2HpRatio = 0.5f;

    [Tooltip("HP 비율이 이 값 이하가 되면 3페이즈 진입 (예: 0.2 = 20%)")]
    public float phase3HpRatio = 0.2f;

    [Header("페이즈별 이동 속도 배율")]
    [Tooltip("1페이즈 기준 이동 속도 (CharacterBase.moveSpeed 원본 값)")]
    private float baseMoveSpeed;

    [Tooltip("2페이즈 이동 속도 배율 (예: 1.2 = 20% 증가)")]
    public float phase2MoveSpeedMultiplier = 1.2f;

    [Tooltip("3페이즈 이동 속도 배율 (예: 1.4 = 40% 증가)")]
    public float phase3MoveSpeedMultiplier = 1.4f;

    [Header("코어 오브젝트 설정")]
    [Tooltip("가슴 쪽 오염 코어 오브젝트")]
    [SerializeField] private GameObject coreObject;

    [Header("피격 연출")]
    [SerializeField] private DamageFeedback damageFeedback;

    [Header("UI 참조")]
    [SerializeField] private BossUIStatus bossUI;

    [Header("브레이크 시스템")]
    [Tooltip("이 페이즈 이상부터 브레이크 시스템 활성화(기본 2페이즈 부터)")]
    public int breakEnableFromPhase = 2;
    [Tooltip("브레이크 발동까지 필요한 피격 횟수")]
    //기본 10회
    public int breakHitThreshold = 10;

    [Tooltip("브레이크 그로기 지속 시간")]
    public float breakGroggyDuration = 5f;

    [Tooltip("그로기 중 추가 피해 비율 (0.2 = 20% 더 받음)")]
    [Range(0f, 5f)] public float groggyExtraDamageRatio = 0.2f;

    [Tooltip("그로기 애니메이션 트리거 이름")]
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

    public int CurrentPhase { get; private set; } = 1;

    private bool phase3FinaleStarted = false;
    private bool phase3FinaleKillDone = false;

    [Header("3페이즈 특수 연출(튜토보스용)")]
    [Tooltip("체크하면 3페이즈 진입 시 전용 연출 + 플레이어 HP 1로")]
    [SerializeField] private bool usePhase3TutorialSequence = false;

    [Tooltip("3페이즈 진입 연출 트리거 이름(추천: Phase3Finale)")]
    [SerializeField] private string phase3TutorialTriggerName = "Phase3Finale";

    [Tooltip("트리거 후 HP를 1로 만드는 딜레이(연출 타이밍 맞추기용)")]
    [SerializeField] private float phase3SetHpDelay = 0.2f;

    private bool phase3SequencePlayed = false;

    protected override void Awake()
    {
        base.Awake();

        baseMoveSpeed = moveSpeed;
        ai = GetComponent<BossAIController>();
        monsterAnim = GetComponent<MonsterAnimation>();

        if (damageFeedback == null)
            damageFeedback = GetComponentInChildren<DamageFeedback>(true);

        if (bossUI == null)
            bossUI = FindObjectOfType<BossUIStatus>();

        if (bossUI != null)
            bossUI.SetBoss(this);

        if (coreObject != null)
            coreObject.SetActive(false);
    }

    //그로기 중 받는 피해 증가 적용
    protected override float GetIncomingDamageMultiplier(DamageInfo info)
    {
        float mul = 1f;

        if (isGroggy)
            mul *= (1f + groggyExtraDamageRatio);

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

        //페이즈 전환 1 -> 2 
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

            // 보스 더 이상 안 맞게(무적)
            StartInvincible(999999f);

            // 전용 트리거 발동
            var anim = GetComponentInChildren<Animator>();
            if (anim != null && !string.IsNullOrEmpty(phase3FinaleTriggerName))
            {
                anim.ResetTrigger(phase3FinaleTriggerName);
                anim.SetTrigger(phase3FinaleTriggerName);
            }

            // AI 멈춤
            var ai = GetComponent<BossAIController>();
            if (ai != null) ai.enabled = false;
        }
    }

    private void EnterPhase(int newPhase)
    {
        if (newPhase <= CurrentPhase) return;

        CurrentPhase = Mathf.Clamp(newPhase, 1, maxPhase);
        ApplyPhaseStats();

        if (CurrentPhase >= 2 && coreObject != null)
        {
            coreObject.SetActive(true);
        }

        if (bossUI != null)
            bossUI.SetBreakVisible(CurrentPhase >= breakEnableFromPhase);
    }

    //페이즈에 따라 스탯 적용
    private void ApplyPhaseStats()
    {
        switch (CurrentPhase)
        {
            case 1:
                moveSpeed = baseMoveSpeed;
                break;
            case 2:
                moveSpeed = baseMoveSpeed * phase2MoveSpeedMultiplier;
                break;
            case 3:
                moveSpeed = baseMoveSpeed * phase3MoveSpeedMultiplier;
                break;
            default:
                moveSpeed = baseMoveSpeed;
                break;
        }
    }

    private void TryAccumulateBreak()
    {
        if (isGroggy) return; // 그로기 중엔 누적 X
        if (CurrentPhase < breakEnableFromPhase) return;
        if (breakHitThreshold <= 0) return;

        breakHitCount++;
        OnBreakHitChanged?.Invoke(breakHitCount, breakHitThreshold);

        bossUI?.UpdateBreak(breakHitCount, breakHitThreshold);

        if (breakHitCount >= breakHitThreshold)
        {
            StartCoroutine(BreakGroggyRoutine());
        }
    }

    private IEnumerator BreakGroggyRoutine()
    {
        if (isGroggy) yield break;

        isGroggy = true;
        OnGroggyChanged?.Invoke(true);

        bossUI?.SetGroggy(true);

        // AI를 Down(그로기)로 넣고, 애니 트리거도 여기서 처리
        if (ai != null)
            ai.EnterBreakGroggy(breakGroggyDuration, breakGroggyTriggerName);

        yield return new WaitForSeconds(breakGroggyDuration);

        // 그로기 끝나면 리셋
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

        // 죽으면 코어 비활성
        if (coreObject != null)
            coreObject.SetActive(false);

        if (bossUI != null)
        {
            bossUI.SetVisible(false);
        }

        monsterAnim?.PlayDie();
    }

    public void Anim_DestroySelf()
    {
        Destroy(gameObject);
    }

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
