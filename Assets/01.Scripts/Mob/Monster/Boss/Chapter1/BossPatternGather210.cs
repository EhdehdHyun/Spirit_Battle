using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossPatternGather210 : BossPatternBase
{
    [Header("랜덤 쿨타임")]
    public float cooldownMin = 3f;
    public float cooldownMax = 6f;
    private float nextReadyTime = 0f;

    [Header("Animator Triggers")]
    [Tooltip("양팔 벌리는 준비 모션 트리거(한 클립이면 이 트리거만 쓰기)")]
    public string openTriggerName = "GatherOpen";

    [Tooltip("전방으로 모으는 모션 트리거(한 클립이면 비워도 됨)")]
    public string closeTriggerName = "GatherClose";

    [Header("Timing")]
    [Tooltip("Open 트리거 이후 Close 트리거까지 대기 시간")]
    public float openDuration = 0.8f;

    [Header("Damage (210도 부채꼴)")]
    public float damage = 25f;
    public float radius = 6f;

    [Tooltip("전방 부채꼴 각도(총각). 210이면 좌105/우105")]
    [Range(10f, 270f)] public float arcAngle = 210f;

    public LayerMask hitMask;
    public Transform originOverride;
    public Vector3 centerOffset = new Vector3(0f, 1.0f, 0f);

    [Header("Performance")]
    public int maxHits = 16;

    [Header("Failsafe")]
    public float patternTimeout = 6f;

    private Animator animator;
    private int _openHash, _closeHash;

    private bool patternFinished = false;
    private Collider[] _overlap;
    private readonly HashSet<int> _hitRootIds = new HashSet<int>();

    protected override void Awake()
    {
        base.Awake();

        nextReadyTime = Time.time + Random.Range(cooldownMin, cooldownMax);

        animator = GetComponentInChildren<Animator>();
        _openHash = !string.IsNullOrEmpty(openTriggerName) ? Animator.StringToHash(openTriggerName) : 0;
        _closeHash = !string.IsNullOrEmpty(closeTriggerName) ? Animator.StringToHash(closeTriggerName) : 0;

        _overlap = new Collider[Mathf.Max(4, maxHits)];
    }

    public override bool CanExecute(Transform target)
    {
        if (boss == null) return false;
        if (target == null) return false;

        if (Time.time < nextReadyTime) return false;

        // 페이즈 조건
        if (boss is BossEnemy be)
        {
            if (be.CurrentPhase < minPhase || be.CurrentPhase > maxPhase)
                return false;
        }

        // 거리 조건
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

        Trigger(_openHash);

        if (openDuration > 0f)
            yield return new WaitForSeconds(openDuration);

        Trigger(_closeHash);

        while (!patternFinished)
        {
            if (Time.time - start >= patternTimeout)
            {
                Debug.LogWarning("[BossPatternGather210] Anim_EndPattern 이벤트 누락 -> 타임아웃 강제 종료", this);
                break;
            }
            yield return null;
        }

        nextReadyTime = Time.time + Random.Range(cooldownMin, cooldownMax);
    }

    public void Anim_DoGatherHit()
    {
        DoDamage();
    }

    public void Anim_210_EndPattern()
    {
        patternFinished = true;
    }

    private void DoDamage()
    {
        Vector3 origin = GetOrigin();
        float half = arcAngle * 0.5f;

        int count = Physics.OverlapSphereNonAlloc(
            origin,
            radius,
            _overlap,
            hitMask,
            QueryTriggerInteraction.Collide
        );

        _hitRootIds.Clear();

        Vector3 fwd = transform.forward;
        fwd.y = 0f;
        if (fwd.sqrMagnitude < 0.0001f) fwd = Vector3.forward;
        fwd.Normalize();

        for (int i = 0; i < count; i++)
        {
            Collider col = _overlap[i];
            if (!col) continue;

            Transform root = col.transform.root;
            int id = root.GetInstanceID();
            if (_hitRootIds.Contains(id)) continue;

            Vector3 to = (col.transform.position - origin);
            to.y = 0f;
            if (to.sqrMagnitude < 0.0001f) continue;

            float ang = Vector3.Angle(fwd, to.normalized);
            if (ang > half) continue;

            _hitRootIds.Add(id);

            var dmg = root.GetComponentInChildren<IDamageable>();
            if (dmg == null) continue;

            Vector3 hitPoint = col.ClosestPoint(origin);
            Vector3 hitNormal = (hitPoint - origin).normalized;

            DamageInfo info = new DamageInfo(damage, hitPoint, hitNormal);
            dmg.TakeDamage(info);
        }
    }

    private Vector3 GetOrigin()
    {
        Transform t = originOverride != null ? originOverride : transform;
        return t.position + centerOffset;
    }

    private void Trigger(int hash)
    {
        if (animator == null) return;
        if (hash == 0) return;

        animator.ResetTrigger(hash);
        animator.SetTrigger(hash);
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 origin = GetOrigin();
        Gizmos.DrawWireSphere(origin, radius);

        Vector3 fwd = transform.forward; fwd.y = 0f;
        if (fwd.sqrMagnitude > 0.0001f) fwd.Normalize();

        float half = arcAngle * 0.5f;
        Vector3 left = Quaternion.Euler(0, -half, 0) * fwd;
        Vector3 right = Quaternion.Euler(0, half, 0) * fwd;

        Gizmos.DrawLine(origin, origin + left * radius);
        Gizmos.DrawLine(origin, origin + right * radius);
    }
}
