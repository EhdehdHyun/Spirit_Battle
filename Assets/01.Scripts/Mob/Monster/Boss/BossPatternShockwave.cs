using System.Collections;
using UnityEngine;

public class BossPatternShockwave : BossPatternBase
{
    [Header("충격파 기본 설정")]
    public float damage = 20f;
    public float radius = 8f;
    public LayerMask hitMask;

    [Header("멀티 웨이브 설정")]
    public int phase1WaveCount = 1;   // 1페이즈 웨이브 수
    public int phase2WaveCount = 2;   // 2페이즈 이상 웨이브 수
    public float waveInterval = 0.4f; // 웨이브 사이 딜레이

    [Header("상태이상 (슬로우) - 추후 구현")]
    public float slowRatio = 0.3f;
    public float slowDuration = 3f;

    [Header("비주얼")]
    public GameObject telegraphObject;     // 바닥 이펙트
    public string slamTriggerName = "Slam";

    private Animator animator;
    private int slamTriggerHash;

    // 현재 웨이브가 끝났는지(애니 이벤트로 알려줄 플래그)
    private bool currentWaveFinished = false;

    protected override void Awake()
    {
        base.Awake();

        animator = GetComponentInChildren<Animator>();

        if (!string.IsNullOrEmpty(slamTriggerName))
            slamTriggerHash = Animator.StringToHash(slamTriggerName);

        if (telegraphObject != null)
            telegraphObject.SetActive(false);
    }

    protected override IEnumerator ExecutePattern()
    {
        if (boss == null) yield break;

        int waveCount = phase1WaveCount;
        if (boss is BossEnemy be && be.CurrentPhase >= 2)
            waveCount = phase2WaveCount;

        for (int i = 0; i < waveCount; i++)
        {
            currentWaveFinished = false;

            // 1) 애니메이션 시작 (팔 들어올리기 시작)
            if (animator != null && slamTriggerHash != 0)
                animator.SetTrigger(slamTriggerHash);

            // 2) 이 이후의 타이밍은 전부 "애니메이션 이벤트"가 담당
            //    아래 while은 "이번 웨이브 끝" 이벤트가 올 때까지 기다리는 역할
            while (!currentWaveFinished)
                yield return null;

            // 3) 다음 웨이브까지 대기
            if (i < waveCount - 1 && waveInterval > 0f)
                yield return new WaitForSeconds(waveInterval);
        }
    }

    // ====== 여기부터는 애니메이션 이벤트에서 호출할 함수들 ======

    /// <summary>애니메이션 중 **텔레그래프를 켜고 싶은 프레임**에 넣을 이벤트</summary>
    public void Anim_ShowTelegraph()
    {
        if (telegraphObject == null || boss == null) return;

        telegraphObject.SetActive(true);
        telegraphObject.transform.position = boss.transform.position;
        telegraphObject.transform.localScale =
            new Vector3(radius * 2f, 1f, radius * 2f);
    }

    /// <summary>텔레그래프를 끄고 싶은 프레임에 넣을 이벤트</summary>
    public void Anim_HideTelegraph()
    {
        if (telegraphObject == null) return;
        telegraphObject.SetActive(false);
    }

    /// <summary>실제 충격파 데미지가 들어가는 프레임에 넣을 이벤트</summary>
    public void Anim_DoShockwave()
    {
        if (boss == null) return;

        Vector3 center = boss.transform.position;
        Collider[] cols = Physics.OverlapSphere(center, radius, hitMask);

        foreach (Collider col in cols)
        {
            IDamageable damageable = col.GetComponentInParent<IDamageable>();
            if (damageable == null) continue;

            Vector3 hitPoint = col.ClosestPoint(center);
            Vector3 hitNormal = (hitPoint - center).normalized;

            DamageInfo info = new DamageInfo(damage, hitPoint, hitNormal);
            damageable.TakeDamage(info);

            // TODO: 여기서 슬로우 상태이상 적용
        }
    }

    public void Anim_EndWave()
    {
        currentWaveFinished = true;
    }
}
