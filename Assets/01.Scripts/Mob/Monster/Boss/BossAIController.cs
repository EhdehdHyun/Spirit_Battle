using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class BossAIController : MonoBehaviour, IParryGroggyController
{
    public enum BossState { Idle, Chase, BasicAttack, Pattern, Down, Dead }

    [Header("이동 설정")]
    [SerializeField] private float stopDistance = 3f;

    [Header("회전 설정")]
    [SerializeField] private bool manualRotation = true;
    [SerializeField] private float rotateSpeedChase = 8f;
    [SerializeField] private float rotateSpeedAttack = 12f;
    [SerializeField] private float rotateMultiplier = 1f;

    [Header("NavMeshAgent 옵션")]
    [SerializeField] private float repathInterval = 0.1f;

    [Header("패턴 시스템")]
    [SerializeField] private BossPatternBase[] patterns;
    [SerializeField] private float patternAllowedDistance = 12f;
    [SerializeField] private float patternMinTimeAfterSessionStart = 0.6f;
    [SerializeField] private float patternGlobalCooldown = 1.0f;

    [Header("Parry Groggy")]
    [SerializeField] private float loseTargetDistance = 20f;
    [SerializeField] private string defaultParryGroggyTrigger = "ParryGroggy";
    private Coroutine groggyCo;

    [Header("Debug")]
    [SerializeField] private bool debugLog = false;

    [SerializeField] private bool debugMoveLock = false;
    private bool canMove = true;
    public bool CanMove => canMove;

    private BossEnemy boss;
    private Transform target;

    private IEnemyAttack basicAttack;
    private EnemyMeleeAttack meleeAttack;
    private MonsterAnimation monsterAnim;
    private NavMeshAgent agent;

    private BossPatternBase currentPattern;
    private Coroutine patternCo;

    private BossState state = BossState.Idle;

    private float nextRepathTime;
    private float sessionStartTime;
    private float nextPatternTime;

    public Transform DebugTarget => target;
    public BossState DebugState => state;
    public bool DebugHasTarget => HasTarget;

    public bool HasTarget => target != null && boss != null && !boss.IsDead;
    public bool IsParryImmune => (boss != null && boss.IsDead) || state == BossState.Down || state == BossState.Dead;

    private void Awake()
    {
        boss = GetComponent<BossEnemy>();
        basicAttack = GetComponent<IEnemyAttack>();
        meleeAttack = GetComponent<EnemyMeleeAttack>();
        monsterAnim = GetComponent<MonsterAnimation>() ?? GetComponentInChildren<MonsterAnimation>();
        agent = GetComponent<NavMeshAgent>();

        if (agent != null)
        {
            agent.updateRotation = !manualRotation;
            agent.autoBraking = true;
            agent.stoppingDistance = stopDistance;
        }
    }

    private void OnEnable()
    {
        if (boss == null) boss = GetComponent<BossEnemy>();
        if (agent == null) agent = GetComponent<NavMeshAgent>();

        if (target == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) target = p.transform;
        }
    }

    private void OnDisable()
    {
        StopAllRuntime();
    }

    public void ResetForReuse(Transform newTarget)
    {
        StopAllRuntime();

        target = newTarget;

        sessionStartTime = Time.time;
        nextRepathTime = 0f;
        nextPatternTime = 0f;

        if (agent != null)
        {
            EnsureOnNavMesh("ResetForReuse");
            agent.isStopped = false;
            agent.ResetPath();
            agent.stoppingDistance = stopDistance;
        }

        if (HasTarget) ChangeState(BossState.Chase);
        else ChangeState(BossState.Idle);

        if (debugLog)
            Debug.Log($"[BossAI] ResetForReuse target={(target ? target.name : "NULL")} state={state}", this);
    }

    private void StopAllRuntime()
    {
        if (groggyCo != null) { StopCoroutine(groggyCo); groggyCo = null; }

        if (patternCo != null) { StopCoroutine(patternCo); patternCo = null; }
        currentPattern = null;

        if (patterns != null)
        {
            foreach (var p in patterns)
                if (p) p.SendMessage("ForceStop", SendMessageOptions.DontRequireReceiver);
        }

        if (agent != null && agent.enabled)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }

        state = BossState.Idle;
    }

    private void Update()
    {
        if (boss == null) return;
        if (boss.IsDead) { ChangeState(BossState.Dead); return; }

        if (agent != null)
            agent.speed = boss.moveSpeed;

        switch (state)
        {
            case BossState.Idle: UpdateIdle(); break;
            case BossState.Chase: UpdateChase(); break;
            case BossState.BasicAttack: UpdateBasicAttack(); break;
            case BossState.Pattern: UpdatePattern(); break;
            case BossState.Down: UpdateDown(); break;
            case BossState.Dead: break;
        }

        UpdateAnimation();
    }

    private void ChangeState(BossState newState)
    {
        if (state == newState) return;

        state = newState;

        switch (state)
        {
            case BossState.Idle:
                StopAgent(clearPath: true);
                break;

            case BossState.Chase:
                ResumeAgent();
                break;

            case BossState.BasicAttack:
                StopAgent(clearPath: true);
                break;

            case BossState.Pattern:
                StopAgent(clearPath: false);
                break;

            case BossState.Down:
                StopAgent(clearPath: true);
                break;

            case BossState.Dead:
                StopAgent(clearPath: true);
                break;
        }
    }

    private void UpdateIdle()
    {
        if (HasTarget) ChangeState(BossState.Chase);
    }

    private void UpdateChase()
    {
        if (!HasTarget) { ChangeState(BossState.Idle); return; }

        float atkRange = GetBasicAttackRange();
        float distAtk = DistToTargetFromAttackOrigin();

        if (distAtk <= atkRange)
        {
            ChangeState(BossState.BasicAttack);
            return;
        }

        // 이동 먼저
        float adjustedStop = Mathf.Min(stopDistance, Mathf.Max(0.1f, atkRange * 0.9f));
        MoveTowardsTarget_Nav(adjustedStop);
        RotateTowardsTarget(rotateSpeedChase);

        if (Time.time - sessionStartTime < patternMinTimeAfterSessionStart) return;
        if (Time.time < nextPatternTime) return;
        if (distAtk > patternAllowedDistance) return;

        if (TryUsePattern())
        {
            nextPatternTime = Time.time + Mathf.Max(0.05f, patternGlobalCooldown);
            return;
        }
    }

    private void UpdateBasicAttack()
    {
        if (!HasTarget) { ChangeState(BossState.Idle); return; }

        float atkRange = GetBasicAttackRange();
        float distAtk = DistToTargetFromAttackOrigin();

        bool isAttacking = (basicAttack != null && basicAttack.IsAttacking);

        if (!isAttacking && distAtk > atkRange * 1.2f)
        {
            ChangeState(BossState.Chase);
            return;
        }

        RotateTowardsTarget(rotateSpeedAttack);
        basicAttack?.TryAttack(target);
    }

    private void UpdatePattern()
    {
        // 패턴 코루틴이 끝나면 상태 복귀
        if (currentPattern != null && currentPattern.IsRunning) return;

        currentPattern = null;
        patternCo = null;

        if (!HasTarget) { ChangeState(BossState.Idle); return; }

        float atkRange = GetBasicAttackRange();
        float distAtk = DistToTargetFromAttackOrigin();

        if (distAtk <= atkRange) ChangeState(BossState.BasicAttack);
        else ChangeState(BossState.Chase);
    }

    private bool TryUsePattern()
    {
        if (patterns == null || patterns.Length == 0) return false;
        if (!HasTarget) return false;

        foreach (var p in patterns)
        {
            if (p == null) continue;
            if (!p.CanExecute(target)) continue;

            currentPattern = p;

            // 패턴 실행
            if (patternCo != null) StopCoroutine(patternCo);
            patternCo = StartCoroutine(CoRunPattern(p));

            ChangeState(BossState.Pattern);
            return true;
        }

        return false;
    }

    private IEnumerator CoRunPattern(BossPatternBase p)
    {
        yield return p.Excute(target);
    }

    private void UpdateDown()
    {
        // 멈춤 유지
    }

    public void EnterBreakGroggy(float duration, string triggerName) => EnterParryGroggy(duration, triggerName);

    public void EnterParryGroggy(float duration, string triggerName)
    {
        if (boss == null || boss.IsDead) return;

        if (groggyCo != null) StopCoroutine(groggyCo);

        ChangeState(BossState.Down);

        var anim = GetComponentInChildren<Animator>();
        string trig = string.IsNullOrEmpty(triggerName) ? defaultParryGroggyTrigger : triggerName;
        if (anim != null && !string.IsNullOrEmpty(trig))
        {
            anim.ResetTrigger(trig);
            anim.SetTrigger(trig);
        }

        groggyCo = StartCoroutine(GroggyRoutine(Mathf.Max(0.05f, duration)));
    }

    private IEnumerator GroggyRoutine(float duration)
    {
        yield return new WaitForSeconds(duration);
        groggyCo = null;

        if (!HasTarget) { ChangeState(BossState.Idle); yield break; }

        float dist = Vector3.Distance(transform.position, target.position);
        if (dist > loseTargetDistance) { ChangeState(BossState.Idle); yield break; }

        float atkRange = GetBasicAttackRange();
        float distAtk = DistToTargetFromAttackOrigin();

        if (distAtk <= atkRange) ChangeState(BossState.BasicAttack);
        else ChangeState(BossState.Chase);
    }

    private float GetBasicAttackRange()
    {
        if (meleeAttack != null) return meleeAttack.hitRadius;
        return boss != null ? boss.attackRange : 2f;
    }

    private Vector3 GetBasicAttackOrigin()
    {
        if (meleeAttack != null && meleeAttack.hitOrigin != null) return meleeAttack.hitOrigin.position;
        return transform.position;
    }

    private float DistToTargetFromAttackOrigin()
    {
        if (!HasTarget) return Mathf.Infinity;
        return Vector3.Distance(GetBasicAttackOrigin(), target.position);
    }

    private void MoveTowardsTarget_Nav(float stopDist)
    {
        if (!HasTarget) return;
        if (agent == null || !agent.enabled) return;
        if (!EnsureOnNavMesh("MoveTowardsTarget_Nav")) return;

        agent.stoppingDistance = Mathf.Max(0f, stopDist);
        agent.isStopped = false;

        // repathInterval에 맞춰서만 갱신
        if (repathInterval > 0f && Time.time < nextRepathTime && agent.hasPath)
            return;

        nextRepathTime = Time.time + Mathf.Max(0f, repathInterval);
        agent.SetDestination(target.position);
    }

    private void StopAgent(bool clearPath)
    {
        if (agent == null || !agent.enabled) return;

        agent.isStopped = true;
        if (clearPath) agent.ResetPath();
    }

    private void ResumeAgent()
    {
        if (agent == null || !agent.enabled) return;

        if (!EnsureOnNavMesh("ResumeAgent")) return;

        agent.isStopped = false;
        // Chase 복귀 시 목적지 한 번 갱신
        if (HasTarget) agent.SetDestination(target.position);
    }

    private void RotateTowardsTarget(float speed)
    {
        if (!manualRotation) return;
        if (!HasTarget) return;
        if (speed <= 0f) return;

        Vector3 toTarget = target.position - transform.position;
        toTarget.y = 0f;
        if (toTarget.sqrMagnitude < 0.0001f) return;

        Quaternion lookRot = Quaternion.LookRotation(toTarget.normalized);
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, speed * rotateMultiplier * Time.deltaTime);
    }

    private void UpdateAnimation()
    {
        if (monsterAnim == null || boss == null) return;

        float speed = 0f;
        if (state == BossState.Chase) speed = boss.moveSpeed;

        bool isChasing = (state == BossState.Chase);
        bool isDead = boss.IsDead;

        monsterAnim.UpdateLocomotion(speed, isChasing, isDead);
    }

    private bool EnsureOnNavMesh(string ctx)
    {
        if (agent == null || !agent.enabled) return false;
        if (agent.isOnNavMesh) return true;

        NavMeshHit hit;
        if (NavMesh.SamplePosition(transform.position, out hit, 5f, NavMesh.AllAreas))
        {
            bool warped = agent.Warp(hit.position);
            if (debugLog)
                Debug.Log($"[BossAI] {ctx}: off-mesh -> Warp({warped}) to {hit.position}", this);
            return agent.isOnNavMesh;
        }

        if (debugLog)
            Debug.LogWarning($"[BossAI] {ctx}: cannot find NavMesh near {transform.position}", this);

        return false;
    }

    public void SetCanMove(bool value)
    {
        if (canMove == value) return;

        canMove = value;

        if (agent == null || !agent.enabled) return;

        if (!canMove)
        {
            agent.isStopped = true;



            if (debugMoveLock)
                Debug.Log($"[BossAI] MoveLock ON  hasPath={agent.hasPath} remDist={agent.remainingDistance:F2}", this);
        }
        else
        {
            if (!EnsureOnNavMesh("SetCanMove(true)")) return;

            agent.isStopped = false;

            if (HasTarget)
                agent.SetDestination(target.position);

            if (debugMoveLock)
                Debug.Log($"[BossAI] MoveLock OFF hasPath={agent.hasPath} remDist={agent.remainingDistance:F2}", this);
        }
    }
}
