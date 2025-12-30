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
    public float idleUpdateInterval = 0.5f;
    public float chaseUpdateInterval = 0.1f;
    public float loseTargetDistance = 20f;

    private EnemyBase enemy;
    private NavMeshAgent agent;
    private IEnemyAttack attack;
    private EnemyMeleeAttack meleeAttack; // hitOrigin/hitRadius 접근용
    private Transform target;
    private MonsterAnimation monsterAnim;

    private AIState state = AIState.Idle;
    private float stateTimer = 0f;

    public bool HasTarget => target != null;

    // ===== 패링(그로기)용 =====
    [Header("Parry Groggy")]
    [SerializeField] private string defaultParryGroggyTrigger = "ParryGroggy";
    private Coroutine groggyCo;

    public bool IsParryImmune => (enemy != null && enemy.IsDead) || state == AIState.Groggy || state == AIState.Dead;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        enemy = GetComponent<EnemyBase>();

        attack = GetComponent<IEnemyAttack>();
        meleeAttack = GetComponent<EnemyMeleeAttack>(); //

        if (enemy == null)
            Debug.LogError($"{name} : EnemyBase가 필요합니다");

        if (attack == null)
            Debug.LogWarning($"{name} : IEnemyAttack 컴포넌트가 없습니다.");

        monsterAnim = GetComponent<MonsterAnimation>() ?? GetComponentInChildren<MonsterAnimation>();

        if (target == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) target = playerObj.transform;
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
            case AIState.Groggy:
            case AIState.Dead:
                if (agent != null) { agent.isStopped = true; agent.ResetPath(); }
                break;
        }

        if (state == AIState.Dead)
            monsterAnim?.PlayDie();
    }

    // hitOrigin/hitRadius 기반 “실제 공격 거리” 계산
    private float GetEffectiveAttackRange()
    {
        if (meleeAttack != null) return meleeAttack.hitRadius;
        return enemy != null ? enemy.attackRange : 2f;
    }

    private Vector3 GetEffectiveAttackOrigin()
    {
        if (meleeAttack != null && meleeAttack.hitOrigin != null) return meleeAttack.hitOrigin.position;
        return transform.position;
    }

    private float DistanceToTargetFromAttackOrigin()
    {
        if (!HasTarget) return Mathf.Infinity;
        return Vector3.Distance(GetEffectiveAttackOrigin(), target.position);
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

        float dist = Vector3.Distance(transform.position, target.position);

        if (dist > loseTargetDistance) ChangeState(AIState.Idle);
        else if (DistanceToTargetFromAttackOrigin() <= GetEffectiveAttackRange()) ChangeState(AIState.Attack);
        else
        {
            ChangeState(AIState.Chase);
            if (agent != null) agent.SetDestination(target.position);
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
            float dist = Vector3.Distance(transform.position, target.position);

            if (enemy != null && dist <= enemy.detectRange)
                ChangeState(AIState.Chase);
        }
    }

    private void UpdateChase()
    {
        if (!HasTarget) { ChangeState(AIState.Idle); return; }

        stateTimer += Time.deltaTime;

        float distCenter = Vector3.Distance(transform.position, target.position);
        if (distCenter > loseTargetDistance) { ChangeState(AIState.Idle); return; }

        float atkRange = GetEffectiveAttackRange();
        float distAtk = DistanceToTargetFromAttackOrigin();

        // 공격 가능 거리(hitOrigin 기준)면 Attack 진입
        if (distAtk <= atkRange)
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

        float atkRange = GetEffectiveAttackRange();
        float distAtk = DistanceToTargetFromAttackOrigin();

        bool isAttacking = (attack != null && attack.IsAttacking);

        // “hitOrigin 기준으로 사거리 밖”이면 다시 추적으로 돌아가서 자리 잡기
        if (!isAttacking && distAtk > atkRange * 1.2f)
        {
            ChangeState(AIState.Chase);
            return;
        }

        // 타겟 쪽 회전
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
        // 그로기 중엔 정지
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

    private void TryFindTarget()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) target = playerObj.transform;
    }

    private void OnDrawGizmosSelected()
    {
        //“공격 원”도 hitOrigin/hitRadius 기준으로 보여주기
        if (meleeAttack == null) meleeAttack = GetComponent<EnemyMeleeAttack>();
        if (enemy == null) enemy = GetComponent<EnemyBase>();

        Vector3 center = transform.position;

        if (enemy != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(center, enemy.detectRange);
        }

        float atkRange = (meleeAttack != null) ? meleeAttack.hitRadius : (enemy != null ? enemy.attackRange : 0f);
        Vector3 atkCenter = (meleeAttack != null && meleeAttack.hitOrigin != null) ? meleeAttack.hitOrigin.position : center;

        if (atkRange > 0f)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(atkCenter, atkRange);
        }

        if (loseTargetDistance > 0f)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(center, loseTargetDistance);
        }
    }
}
