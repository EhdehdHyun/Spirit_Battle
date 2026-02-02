using System.Collections;
using UnityEngine;

public class BossPatternSlamSlam : BossPatternBase
{
    [Header("Cooldown")]
    public float cooldownMin = 3f;
    public float cooldownMax = 6f;
    private float nextReadyTime = 0f;

    [Header("Animator Triggers")]
    [Tooltip("패턴 시작 트리거")]
    public string slam2TriggerName = "SlamX2";

    [Header("Telegraph (Circle)")]
    public GameObject telegraphObject;
    public Transform centerOverride;
    [SerializeField] private float telegraphYOffset = 0.02f;

    [Header("Slam#1")]
    public float slam1Damage = 20f;
    public float slam1Radius = 8f;
    public LayerMask slam1HitMask;

    [Header("Slam#2")]
    public float slam2Damage = 25f;
    public float slam2Radius = 9f;
    public LayerMask slam2HitMask;

    [Header("Failsafe")]
    public float patternTimeout = 10f;

    private Animator animator;
    private int _slam2Hash;

    private bool patternFinished;

    protected override void Awake()
    {
        base.Awake();

        nextReadyTime = Time.time + Random.Range(cooldownMin, cooldownMax);

        animator = GetComponentInChildren<Animator>();
        _slam2Hash = !string.IsNullOrEmpty(slam2TriggerName) ? Animator.StringToHash(slam2TriggerName) : 0;

        if (telegraphObject != null)
            telegraphObject.SetActive(false);
    }

    public override bool CanExecute(Transform target)
    {
        if (boss == null || target == null) return false;
        if (Time.time < nextReadyTime) return false;

        if (boss is BossEnemy be)
        {
            if (be.CurrentPhase < minPhase || be.CurrentPhase > maxPhase)
                return false;
        }

        float dist = Vector3.Distance(boss.transform.position, target.position);
        if (minUseDistance > 0f && dist < minUseDistance) return false;
        if (maxUseDistance > 0f && dist > maxUseDistance) return false;

        return true;
    }

    protected override IEnumerator ExecutePattern()
    {
        if (boss == null || animator == null) yield break;

        patternFinished = false;
        float start = Time.time;

        Trigger(_slam2Hash);

        while (!patternFinished)
        {
            if (Time.time - start >= patternTimeout)
            {
                Debug.LogWarning("[BossPatternSlamSlam] End 이벤트 누락 -> 타임아웃 강제 종료", this);
                break;
            }
            yield return null;
        }

        SetTelegraph(false, 1f);

        nextReadyTime = Time.time + Random.Range(cooldownMin, cooldownMax);
    }

    // 1타 텔레그래프
    public void Anim_SlamSlam_ShowTelegraph1() => SetTelegraph(true, slam1Radius);
    public void Anim_SlamSlam_HideTelegraph1() => SetTelegraph(false, 1f);

    // 1타 충격파
    public void Anim_SlamSlam_DoShockwave1() => DoShockwaveDamage(slam1Damage, slam1Radius, slam1HitMask);

    // 2타 텔레그래프
    public void Anim_SlamSlam_ShowTelegraph2() => SetTelegraph(true, slam2Radius);
    public void Anim_SlamSlam_HideTelegraph2() => SetTelegraph(false, 1f);

    // 2타 충격파
    public void Anim_SlamSlam_DoShockwave2() => DoShockwaveDamage(slam2Damage, slam2Radius, slam2HitMask);

    // 패턴 끝
    public void Anim_SlamSlam_EndPattern()
    {
        patternFinished = true;
    }

    // ===== Internals =====

    private void DoShockwaveDamage(float damage, float radius, LayerMask mask)
    {
        Vector3 center = GetCenterPosition();

        Collider[] cols = Physics.OverlapSphere(center, radius, mask, QueryTriggerInteraction.Collide);

        foreach (Collider col in cols)
        {
            PhysicsCharacter pc = col.GetComponentInParent<PhysicsCharacter>();
            if (pc != null)
            {
                bool airborne = !pc.IsGrounded && (pc.IsFalling || pc.Velocity.y > 0.1f);
                if (airborne) continue;
            }

            IDamageable dmgable = col.GetComponentInParent<IDamageable>();
            if (dmgable == null) continue;

            Vector3 hitPoint = col.ClosestPoint(center);
            Vector3 hitNormal = (hitPoint - center).normalized;

            DamageInfo info = new DamageInfo(damage, hitPoint, hitNormal);
            dmgable.TakeDamage(info);
        }
    }

    private void SetTelegraph(bool on, float radius)
    {
        if (telegraphObject == null) return;

        if (!on)
        {
            telegraphObject.SetActive(false);
            return;
        }

        Vector3 center = GetCenterPosition();
        center.y += telegraphYOffset;

        telegraphObject.SetActive(true);
        telegraphObject.transform.position = center;

        float diameter = radius * 2f;

        Vector3 s = telegraphObject.transform.localScale;
        s.x = diameter;
        s.y = diameter;
        telegraphObject.transform.localScale = s;

    }

    private Vector3 GetCenterPosition()
    {
        Transform t = centerOverride != null ? centerOverride : transform;
        return t.position;
    }

    private void Trigger(int hash)
    {
        if (animator == null) return;
        if (hash == 0) return;

        animator.ResetTrigger(hash);
        animator.SetTrigger(hash);
    }
}
