using System.Collections;
using UnityEngine;

public class BossPatternSlamPunchCombo : BossPatternBase
{
    [Header("랜덤 쿨타임")]
    public float cooldownMin = 3f;
    public float cooldownMax = 6f;
    private float nextReadyTime = 0f;

    [Header("Animator Triggers")]
    [Tooltip("준비 모션 트리거")]
    public string prepareTriggerName = "SlamPrepare";
    [Tooltip("내려치기(지면강타) 트리거")]
    public string slamTriggerName = "Slam";
    [Tooltip("정권 트리거")]
    public string punchTriggerName = "Punch";

    [Header("Timing")]
    [Tooltip("준비 모션 시작 후 이 시간 뒤 Slam 트리거 발동")]
    public float prepareDuration = 1.2f;

    [Tooltip("Slam 임팩트(충격파 이벤트) 이후 Punch 트리거까지 대기 시간")]
    public float punchDelayAfterSlamImpact = 0.35f;

    [Tooltip("true면 Slam 임팩트 이벤트가 찍힌 뒤에만 Punch로 넘어감")]
    public bool waitSlamImpactBeforePunch = true;

    [Header("Telegraph (원형)")]
    [Tooltip("바닥 원형 텔레그래프 오브젝트")]
    public GameObject telegraphObject;

    // 바닥 깜빡임 방지
    [SerializeField] private float telegraphYOffset = 0.02f;

    [Tooltip("텔레그래프 중심 위치(없으면 보스 위치)")]
    public Transform slamCenterOverride;

    [Header("Slam Shockwave")]
    public float slamDamage = 20f;
    public float slamRadius = 8f;
    public LayerMask slamHitMask;

    [Header("Punch Damage (범위 안이면 무조건 데미지)")]
    [Tooltip("정권 판정 기준(없으면 보스 위치)")]
    public Transform punchOriginOverride;

    public float punchDamage = 15f;
    public float punchRadius = 3.5f;

    [Tooltip("정면 부채꼴 각도(총각). 예: 90이면 좌45/우45")]
    [Range(10f, 210f)] public float punchArc = 90f;

    public LayerMask punchHitMask;
    public Vector3 punchCenterOffset = new Vector3(0f, 1.0f, 0f);

    [Header("Failsafe (이벤트 누락 방지)")]
    [Tooltip("Slam 임팩트 이벤트가 안 찍혔을 때 강제로 넘어가기까지 최대 대기")]
    public float slamImpactTimeout = 2.5f;

    [Tooltip("패턴 전체 최대 실행 시간(이벤트 누락으로 영원히 안 끝나는 것 방지)")]
    public float patternTimeout = 8f;

    private Animator animator;
    private int _prepareHash, _slamHash, _punchHash;

    private bool slamImpactDone = false;
    private bool patternFinished = false;

    protected override void Awake()
    {
        base.Awake();

        nextReadyTime = Time.time + Random.Range(cooldownMin, cooldownMax);

        animator = GetComponentInChildren<Animator>();
        _prepareHash = !string.IsNullOrEmpty(prepareTriggerName) ? Animator.StringToHash(prepareTriggerName) : 0;
        _slamHash = !string.IsNullOrEmpty(slamTriggerName) ? Animator.StringToHash(slamTriggerName) : 0;
        _punchHash = !string.IsNullOrEmpty(punchTriggerName) ? Animator.StringToHash(punchTriggerName) : 0;

        if (telegraphObject != null)
            telegraphObject.SetActive(false);
    }

    public override bool CanExecute(Transform target)
    {
        if (boss == null) return false;
        if (target == null) return false;

        if (Time.time < nextReadyTime) return false;

        if (boss is BossEnemy be)
        {
            if (be.CurrentPhase < minPhase || be.CurrentPhase > maxPhase)
                return false;
        }

        float dist = Vector3.Distance(boss.transform.position, target.position); //거리 조건
        if (minUseDistance > 0f && dist < minUseDistance) return false;
        if (maxUseDistance > 0f && dist > maxUseDistance) return false;

        return true;
    }

    protected override IEnumerator ExecutePattern()
    {
        if (boss == null) yield break;
        if (animator == null) yield break;

        slamImpactDone = false;
        patternFinished = false;

        Trigger(_prepareHash);

        ShowTelegraph();

        float startTime = Time.time;

        if (prepareDuration > 0f)
            yield return new WaitForSeconds(prepareDuration);

        Trigger(_slamHash);

        if (waitSlamImpactBeforePunch)
        {
            float waitStart = Time.time;
            while (!slamImpactDone)
            {
                if (Time.time - waitStart >= slamImpactTimeout)
                {
                    slamImpactDone = true;
                    Debug.LogWarning("[BossPatternSlamPunchCombo] Slam 임팩트 이벤트(Anim_DoSlamShockwave)가 안 찍혀서 타임아웃으로 진행합니다.", this);
                    break;
                }
                yield return null;
            }
        }

        if (punchDelayAfterSlamImpact > 0f)
            yield return new WaitForSeconds(punchDelayAfterSlamImpact);

        Trigger(_punchHash);

        while (!patternFinished)
        {
            if (Time.time - startTime >= patternTimeout)
            {
                Debug.LogWarning("[BossPatternSlamPunchCombo] 패턴 종료 이벤트(Anim_EndPattern)가 안 찍혀서 타임아웃으로 강제 종료합니다.", this);
                break;
            }
            yield return null;
        }

        HideTelegraph();

        // 다음 랜덤 쿨타임
        nextReadyTime = Time.time + Random.Range(cooldownMin, cooldownMax);
    }

    // Slam 임팩트 프레임에 호출
    public void Anim_DoSlamShockwave()
    {
        slamImpactDone = true;

        HideTelegraph();

        DoSlamShockwaveDamage();
    }

    // Punch 타격 프레임에 호출
    public void Anim_DoPunchDamage()
    {
        DoPunchDamage();
    }

    // Punch 애니 마지막 프레임에 호출
    public void Anim_EndPattern()
    {
        patternFinished = true;
        HideTelegraph();
    }

    public void Anim_TriggerPunchNow()
    {
        Trigger(_punchHash);
    }

    private void DoSlamShockwaveDamage()
    {
        Vector3 center = GetSlamCenter();

        Collider[] cols = Physics.OverlapSphere(center, slamRadius, slamHitMask, QueryTriggerInteraction.Collide);
        foreach (var col in cols)
        {
            if (!col) continue;

            PhysicsCharacter pc = col.GetComponentInParent<PhysicsCharacter>();
            if (pc != null)
            {
                bool airborne = !pc.IsGrounded && (pc.IsFalling || pc.Velocity.y > 0.1f);
                if (airborne) continue;
            }

            IDamageable dmg = col.GetComponentInParent<IDamageable>();
            if (dmg == null) continue;

            Vector3 hitPoint = col.ClosestPoint(center);
            Vector3 hitNormal = (hitPoint - center).normalized;

            DamageInfo info = new DamageInfo(slamDamage, hitPoint, hitNormal);
            dmg.TakeDamage(info);
        }
    }

    private void DoPunchDamage()
    {
        Vector3 origin = GetPunchCenter();
        Vector3 fwd = transform.forward;
        fwd.y = 0f;
        if (fwd.sqrMagnitude < 0.0001f) fwd = Vector3.forward;
        fwd.Normalize();

        float half = punchArc * 0.5f;

        Collider[] cols = Physics.OverlapSphere(origin, punchRadius, punchHitMask, QueryTriggerInteraction.Collide);
        foreach (var col in cols)
        {
            if (!col) continue;

            IDamageable dmg = col.GetComponentInParent<IDamageable>();
            if (dmg == null) continue;

            Vector3 to = col.transform.position - origin;
            to.y = 0f;
            if (to.sqrMagnitude < 0.0001f) continue;

            float ang = Vector3.Angle(fwd, to.normalized);
            if (ang > half) continue;

            Vector3 hitPoint = col.ClosestPoint(origin);
            Vector3 hitNormal = (hitPoint - origin).normalized;

            DamageInfo info = new DamageInfo(punchDamage, hitPoint, hitNormal);
            dmg.TakeDamage(info);
        }
    }

    private void ShowTelegraph() => SetTelegraph(true, slamRadius);
    private void HideTelegraph() => SetTelegraph(false, 1f);

    private Vector3 GetSlamCenter()
    {
        if (slamCenterOverride != null) return slamCenterOverride.position;
        if (boss != null) return boss.transform.position;
        return transform.position;
    }

    private Vector3 GetPunchCenter()
    {
        Transform t = punchOriginOverride != null ? punchOriginOverride : transform;
        return t.position + punchCenterOffset;
    }

    private void Trigger(int hash)
    {
        if (animator == null) return;
        if (hash == 0) return;

        animator.ResetTrigger(hash);
        animator.SetTrigger(hash);
    }

    private void SetTelegraph(bool on, float radius)
    {
        if (telegraphObject == null) return;

        if (!on)
        {
            telegraphObject.SetActive(false);
            return;
        }

        Vector3 center = GetSlamCenter();
        center.y += telegraphYOffset;

        telegraphObject.SetActive(true);
        telegraphObject.transform.position = center;

        float diameter = radius * 2f;

        Vector3 s = telegraphObject.transform.localScale;
        s.x = diameter;
        s.y = diameter;
        telegraphObject.transform.localScale = s;

        telegraphObject.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        // Slam 원형
        Gizmos.DrawWireSphere(GetSlamCenter(), slamRadius);

        // Punch 부채꼴(대충)
        Vector3 origin = GetPunchCenter();
        Gizmos.DrawWireSphere(origin, punchRadius);

        Vector3 fwd = transform.forward; fwd.y = 0f;
        if (fwd.sqrMagnitude > 0.0001f) fwd.Normalize();

        float half = punchArc * 0.5f;
        Vector3 left = Quaternion.Euler(0, -half, 0) * fwd;
        Vector3 right = Quaternion.Euler(0, half, 0) * fwd;

        Gizmos.DrawLine(origin, origin + left * punchRadius);
        Gizmos.DrawLine(origin, origin + right * punchRadius);
    }
#endif
}
