using System.Collections;
using UnityEngine;

public class BossPatternSlamTornado : BossPatternBase
{
    [Header("Cooldown")]
    public float cooldownMin = 3f;
    public float cooldownMax = 6f;
    private float nextReadyTime = 0f;

    [Header("Animator Triggers")]
    [Tooltip("지면강타 트리거(패턴 시작 트리거)")]
    public string slamTriggerName = "Slam";

    [Tooltip("어퍼컷 트리거(한 클립이면 비워도 됨)")]
    public string uppercutTriggerName = "Uppercut";

    [Tooltip("어퍼컷 애니메이션 타이밍(0이면 사용 안 함)")]
    public float uppercutDelay = 0f;

    [Header("Telegraph (Circle)")]
    public GameObject telegraphObject; // 빨간 원
    public Transform centerOverride;
    public float slamRadius = 8f;

    [SerializeField] private float telegraphYOffset = 0.02f;

    [Header("Shockwave Damage")]
    public float slamDamage = 20f;
    public LayerMask slamHitMask;

    [Header("Tornado Projectile")]
    public BossTornadoProjectile tornadoPrefab;
    public Transform tornadoSpawn;
    public float tornadoDamage = 25f;
    public LayerMask tornadoHitMask;
    public float tornadoSpeed = 10f;
    public float tornadoDistance = 6f;

    [Header("Failsafe")]
    public float patternTimeout = 8f;

    private Animator animator;
    private int _slamHash, _uppercutHash;

    private bool patternFinished;
    private Coroutine uppercutCo;

    protected override void Awake()
    {
        base.Awake();

        nextReadyTime = Time.time + Random.Range(cooldownMin, cooldownMax);

        animator = GetComponentInChildren<Animator>();
        _slamHash = !string.IsNullOrEmpty(slamTriggerName) ? Animator.StringToHash(slamTriggerName) : 0;
        _uppercutHash = !string.IsNullOrEmpty(uppercutTriggerName) ? Animator.StringToHash(uppercutTriggerName) : 0;

        if (telegraphObject != null)
            telegraphObject.SetActive(false);
    }

    public override bool CanExecute(Transform target)
    {
        if (boss == null || target == null) return false;
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

        Trigger(_slamHash);

        if (_uppercutHash != 0 && uppercutDelay > 0f)
        {
            if (uppercutCo != null) StopCoroutine(uppercutCo);
            uppercutCo = StartCoroutine(CoTriggerUppercutAfterDelay(uppercutDelay));
        }

        while (!patternFinished)
        {
            if (Time.time - start >= patternTimeout)
            {
                Debug.LogWarning("[BossPatternSlamTornado] End 이벤트 누락 -> 타임아웃 강제 종료", this);
                break;
            }
            yield return null;
        }

        if (uppercutCo != null) { StopCoroutine(uppercutCo); uppercutCo = null; }
        SetTelegraph(false, 1f);

        nextReadyTime = Time.time + Random.Range(cooldownMin, cooldownMax);
    }

    private IEnumerator CoTriggerUppercutAfterDelay(float t)
    {
        yield return new WaitForSeconds(t);
        if (patternFinished) yield break;
        Trigger(_uppercutHash);
        uppercutCo = null;
    }

    public void Anim_SlamTornado_ShowTelegraph() => SetTelegraph(true, slamRadius);
    public void Anim_SlamTornado_HideTelegraph() => SetTelegraph(false, 1f);

    public void Anim_SlamTornado_DoShockwave()
    {
        DoShockwaveDamage();
    }

    public void Anim_SlamTornado_TriggerUppercut()
    {
        Trigger(_uppercutHash);
    }

    // 애니에서 회오리 발사 프레임에 넣을 이벤트
    public void Anim_SlamTornado_FireTornado()
    {
        SpawnTornado();
    }

    // 애니 마지막 프레임에 넣을 이벤트
    public void Anim_SlamTornado_EndPattern()
    {
        patternFinished = true;
    }

    private void SpawnTornado()
    {
        if (!tornadoPrefab) return;

        Transform sp = tornadoSpawn != null ? tornadoSpawn : (centerOverride != null ? centerOverride : transform);

        Vector3 fwd = transform.forward;
        fwd.y = 0f;
        if (fwd.sqrMagnitude < 0.0001f) fwd = Vector3.forward;
        fwd.Normalize();

        var proj = Instantiate(tornadoPrefab, sp.position, Quaternion.LookRotation(fwd, Vector3.up));
        proj.Init(
            owner: transform,
            forward: fwd,
            spd: tornadoSpeed,
            dist: tornadoDistance,
            dmg: tornadoDamage,
            mask: tornadoHitMask
        );
    }

    private void DoShockwaveDamage()
    {
        Vector3 center = GetCenterPosition();

        Collider[] cols = Physics.OverlapSphere(center, slamRadius, slamHitMask, QueryTriggerInteraction.Collide);

        foreach (Collider col in cols)
        {
            PhysicsCharacter pc = col.GetComponentInParent<PhysicsCharacter>();

            // 플레이어가 진짜 공중이면 충격파 무시
            if (pc != null)
            {
                bool airborne = !pc.IsGrounded && (pc.IsFalling || pc.Velocity.y > 0.1f);
                if (airborne) continue;
            }

            IDamageable dmgable = col.GetComponentInParent<IDamageable>();
            if (dmgable == null) continue;

            Vector3 hitPoint = col.ClosestPoint(center);
            Vector3 hitNormal = (hitPoint - center).normalized;

            DamageInfo info = new DamageInfo(slamDamage, hitPoint, hitNormal);
            dmgable.TakeDamage(info);
        }
    }

    private Vector3 GetCenterPosition()
    {
        Transform t = centerOverride != null ? centerOverride : transform;
        return t.position;
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

    private void Trigger(int hash)
    {
        if (animator == null) return;
        if (hash == 0) return;

        animator.ResetTrigger(hash);
        animator.SetTrigger(hash);
    }
}
