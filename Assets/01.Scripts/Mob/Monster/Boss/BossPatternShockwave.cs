using System.Collections;
using UnityEngine;
using UnityEngine.SocialPlatforms.GameCenter;

public class BossPatternShockwave : BossPatternBase
{
    [Header("충격파 중심 설정")]
    [Tooltip("충격파의 기준 위치(비워두면 보스 본체 위치 사용)")]
    public Transform centerOverride;

    [Header("충격파 기본 설정")]
    public float damage = 20f;
    public float radius = 8f;
    public LayerMask hitMask;

    [Header("랜덤 쿨타임")]
    public float cooldownMin = 3f;
    public float cooldownMax = 6f;

    private float nextReadyTime = 0f;

    [Header("상태이상 (슬로우) - 추후 구현")]
    public float slowRatio = 0.3f;
    public float slowDuration = 3f;

    [Header("비주얼")]
    public GameObject telegraphObject; // 바닥 이펙트
    public string slamTriggerName = "Slam";

    private Animator animator;
    private int slamTriggerHash;

    // 현재 웨이브가 끝났는지(애니 이벤트로 알려줄 플래그)
    private bool currentWaveFinished = false;

    protected override void Awake()
    {
        base.Awake();

        nextReadyTime = Time.time + Random.Range(cooldownMin, cooldownMax);

        animator = GetComponentInChildren<Animator>();

        if (!string.IsNullOrEmpty(slamTriggerName))
            slamTriggerHash = Animator.StringToHash(slamTriggerName);

        if (telegraphObject != null)
            telegraphObject.SetActive(false);
    }

    protected override IEnumerator ExecutePattern()
    {
        if (boss == null) yield break;

        currentWaveFinished = false;

        // 애니메이션 시작
        if (animator != null && slamTriggerHash != 0)
            animator.SetTrigger(slamTriggerHash);

        // 애니 마지막 프레임(또는 종료 시점)에 Anim_EndWave() 이벤트를 넣어서 끝내기
        while (!currentWaveFinished)
            yield return null;

        // 다음 랜덤 쿨타임 예약
        nextReadyTime = Time.time + Random.Range(cooldownMin, cooldownMax);
    }

    public override bool CanExecute(Transform target)
    {
        // 랜덤 쿨타임 체크
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

    //  애니메이션 중 텔레그래프를 켜고 싶은 프레임에 넣을 이벤트
    public void Anim_ShowTelegraph()
    {
        if (telegraphObject == null || boss == null) return;

        Vector3 center = GetCenterPosotion();

        telegraphObject.SetActive(true);
        telegraphObject.transform.position = center;
        telegraphObject.transform.localScale =
            new Vector3(radius * 2f, 1f, radius * 2f);
    }

    //텔레그래프를 끄고 싶은 프레임에 넣을 이벤트
    public void Anim_HideTelegraph()
    {
        if (telegraphObject == null) return;
        telegraphObject.SetActive(false);
    }

    //실제 충격파 데미지가 들어가는 프레임에 넣을 이벤트
    public void Anim_DoShockwave()
    {
        if (boss == null) return;

        Vector3 center = GetCenterPosotion();
        Collider[] cols = Physics.OverlapSphere(center, radius, hitMask, QueryTriggerInteraction.Collide);

        foreach (Collider col in cols)
        {
            PhysicsCharacter pc = col.GetComponentInParent<PhysicsCharacter>();

            // 공중 회피 "진짜 공중"일 때만 스킵 (그라운드 체크가 순간 튀는 프레임 방지)
            if (pc != null)
            {
                bool airborne = !pc.IsGrounded && (pc.IsFalling || pc.Velocity.y > 0.1f);
                if (airborne) continue;
            }

            IDamageable damageable = col.GetComponentInParent<IDamageable>();
            if (damageable == null) continue;

            Vector3 hitPoint = col.ClosestPoint(center);
            Vector3 hitNormal = (hitPoint - center).normalized;

            DamageInfo info = new DamageInfo(damage, hitPoint, hitNormal);
            damageable.TakeDamage(info);

            // TODO: 슬로우 적용
        }
    }

    public void Anim_EndWave()
    {
        currentWaveFinished = true;
    }

    private Vector3 GetCenterPosotion()
    {
        if (centerOverride != null) return centerOverride.position;

        if (boss != null) return boss.transform.position;

        return transform.position;
    }

    private void OnDrawGizmosSelected()
    {
        if (boss == null)
            boss = GetComponent<BossEnemy>();

        Vector3 center = GetCenterPosotion();

        Gizmos.color = new Color(1f, 0f, 0f, 0.3f);

        Gizmos.DrawWireSphere(center, radius);
    }
}
