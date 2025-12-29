using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class EnemyAIController : MonoBehaviour, IParryGroggyController
{
    public enum AIState
    {
        Idle,
        Chase,
        Attack,
        Groggy,
        Dead
    }

    [Header("AI 설정")]
    [Tooltip("idle 상태에서 타겟 탐색 주기")]
    public float idleUpdateInterval = 0.5f;
    [Tooltip("chase 상태에서 목적지를 갱신하는 주기")]
    public float chaseUpdateInterval = 0.1f;
    [Tooltip("더 이상 안 쫓아오는 거리")]
    public float loseTargetDistance = 20f;

    private EnemyBase enemy;
    private NavMeshAgent agent;
    private IEnemyAttack attack;
    private EnemyMeleeAttack meleeAttack; // ✅ hitOrigin/hitRadius 접근용
    private Transform target;
    private MonsterAnimation monsterAnim;

    private AIState state = AIState.Idle;
    private float stateTimer = 0f;

    public bool HasTarget => target != null;

    // ===== 패링(그로기)용 =====
    [Header("Parry Groggy")]
    [SerializeField] private string defaultParryGroggyTrigger = "ParryGroggy";
    private Coroutine groggyCo;

    // IParryGroggyController
    public bool IsParryImmune => (enemy != null && enemy.IsDead) || state == AIState.Groggy || state == AIState.Dead;

    // ✅ 공격 기준(핵심): hitOrigin/hitRadius 우선, 없으면 fallback
    private Transform AttackOrigin
    {
        get
        {
            if (meleeAttack != null && meleeAttack.hitOrigin != null) return meleeAttack.hitOrigin;
            return transform;
        }
    }

    private float AttackRange
    {
        get
        {
            if (meleeAttack != null) return meleeAttack.hitRadius;
            return enemy != null ? enemy.attackRange : 0f; // fallback
        }
    }

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        enemy = GetComponent<EnemyBase>();
        attack = GetComponent<IEnemyAttack>();
        meleeAttack = GetComponent<EnemyMeleeAttack>(); // ✅ 있으면 공격거리 통일

        if (enemy == null)
            Debug.LogError($"{name} : EnemyBase가 필요합니다");

        if (attack == null)
            Debug.LogWarning($"{name} : IEnemyAttack 컴포넌트가 없습니다.");

        monsterAnim = GetComponent<MonsterAnimation>() ?? GetComponentInChildren<MonsterAnimation>();

        if (target == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                target = playerObj.transform;
        }

        if (enemy != null && agent != null)
            agent.speed = enemy.moveSpeed;
    }

    private void Update()
    {
        if (enemy == null) return;

        if (enemy.IsDead)
            ChangeState(AIState.Dead);

        switch (state)
        {
            case AIState.Idle: UpdateIdle(); break;
            case AIState.Chase: UpdateChase(); break;
            case AIState.Attack: UpdateAttack(); break;
            case AIState.Groggy: UpdateGroggy(); break;
            case AIState.Dead: UpdateDead(); break;
        }

        UpdateAnimation();
    }

    private void ChangeState(AIState newState)
    {
        if (state == newState) return;

        state = newState;
        stateTimer = 0f;

        switch (state)
        {
            case AIState.Idle:
                if (agent != null) { agent.isStopped = true; agent.ResetPath(); }
                break;

            case AIState.Chase:
                if (agent != null) agent.isStopped = false;
                break;

            case AIState.Attack:
                if (agent != null) { agent.isStopped = true; agent.ResetPath(); }
                break;

            case AIState.Groggy:
                if (agent != null) { agent.isStopped = true; agent.ResetPath(); }
                break;

            case AIState.Dead:
                if (agent != null) { agent.isStopped = true; agent.ResetPath(); }
                monsterAnim?.PlayDie();
                break;
        }
    }

    // ====== IParryGroggyController 구현 ======
    public void EnterParryGroggy(float duration, string triggerName)
    {
        if (enemy == null || enemy.IsDead) return;

        if (groggyCo != null) StopCoroutine(groggyCo);

        ChangeState(AIState.Groggy);

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

        if (!HasTarget) { ChangeState(AIState.Idle); yield break; }

        float dist = DistanceToTarget(); // ✅ 추적 포기/감지 등은 몬스터 기준이 자연스러움
        if (dist > loseTargetDistance)
        {
            ChangeState(AIState.Idle);
        }
        else
        {
            float atkDist = DistanceToTargetFromAttackOrigin(); // ✅ 공격 여부는 hitOrigin 기준
            if (atkDist <= AttackRange)
                ChangeState(AIState.Attack);
            else
            {
                ChangeState(AIState.Chase);
                if (agent != null) agent.SetDestination(target.position);
            }
        }
    }

    // ====== 상태별 Update ======
    private void UpdateIdle()
    {
        stateTimer += Time.deltaTime;

        if (!HasTarget)
        {
            TryFindTarget();
            return;
        }

        if (stateTimer >= idleUpdateInterval)
        {
            stateTimer = 0f;

            float dist = DistanceToTarget();
            if (dist <= enemy.detectRange)
                ChangeState(AIState.Chase);
        }
    }

    private void UpdateChase()
    {
        if (!HasTarget) { ChangeState(AIState.Idle); return; }

        stateTimer += Time.deltaTime;

        float dist = DistanceToTarget();
        if (dist > loseTargetDistance) { ChangeState(AIState.Idle); return; }

        // ✅ 공격 전환은 hitOrigin 기준으로!
        float atkDist = DistanceToTargetFromAttackOrigin();
        if (atkDist <= AttackRange)
        {
            ChangeState(AIState.Attack);
            return;
        }

        if (stateTimer >= chaseUpdateInterval)
        {
            stateTimer = 0f;
            if (agent != null) agent.SetDestination(target.position);
        }
    }

    private void UpdateAttack()
    {
        if (!HasTarget) { ChangeState(AIState.Idle); return; }

        bool isAttacking = (attack != null && attack.IsAttacking);

        // ✅ 공격 유지/이탈 판단도 hitOrigin 기준으로!
        float atkDist = DistanceToTargetFromAttackOrigin();
        float atkRange = AttackRange;

        if (!isAttacking && atkDist > atkRange * 1.2f)
        {
            ChangeState(AIState.Chase);
            return;
        }

        // 회전
        Vector3 dir = (target.position - transform.position);
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.01f)
        {
            Quaternion lookRot = Quaternion.LookRotation(dir.normalized);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, 10f * Time.deltaTime);
        }

        attack?.TryAttack(target);
    }

    private void UpdateGroggy()
    {
        // 그로기 중엔 아무 것도 안 함
    }

    private void UpdateAnimation()
    {
        if (monsterAnim == null || agent == null || enemy == null) return;

        float speed = (state == AIState.Groggy || state == AIState.Dead) ? 0f : agent.velocity.magnitude;
        bool isChasing = (state == AIState.Chase);
        bool isDead = enemy.IsDead;

        monsterAnim.UpdateLocomotion(speed, isChasing, isDead);
    }

    private void UpdateDead() { }

    private float DistanceToTarget()
    {
        if (!HasTarget) return Mathf.Infinity;
        return Vector3.Distance(transform.position, target.position);
    }

    private float DistanceToTargetFromAttackOrigin()
    {
        if (!HasTarget) return Mathf.Infinity;
        return Vector3.Distance(AttackOrigin.position, target.position);
    }

    private void TryFindTarget()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            target = playerObj.transform;
    }

    private void OnDrawGizmosSelected()
    {
        if (enemy == null) enemy = GetComponent<EnemyBase>();

        Vector3 center = transform.position;

        // 감지 범위
        if (enemy != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(center, enemy.detectRange);
        }

        // 공격 범위(✅ hitOrigin 기준으로!)
        Gizmos.color = Color.red;
        Vector3 atkCenter = AttackOrigin != null ? AttackOrigin.position : transform.position;
        float atkRange = AttackRange;
        if (atkRange > 0f)
            Gizmos.DrawWireSphere(atkCenter, atkRange);

        // 포기 거리
        Gizmos.color = Color.cyan;
        if (loseTargetDistance > 0f)
            Gizmos.DrawWireSphere(center, loseTargetDistance);
    }
}
