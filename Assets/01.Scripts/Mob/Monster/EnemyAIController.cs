using Unity.Mathematics;
using UnityEngine;
using UnityEngine.AI;

public class EnemyAIController : MonoBehaviour
{
    public enum AIState
    {
        Idle, //대기
        Chase, //추적
        Attack, //공격
        Dead // 유다희
    }

    [Header("AI 설정")]
    public float idleUpdateInterval = 0.5f; //idle 상태에서 타겟 탐색 주기
    public float chaseUpdateInterval = 0.1f; //chase 상태에서 목적지를 갱신하는 주기
    public float loseTargetDistance = 20f;

    private EnemyBase enemy; //몬스터 기본 스탯/상태
    private NavMeshAgent agent;
    private IEnemyAttack attack;
    private Transform target;

    private AIState state = AIState.Idle;
    private float stateTimer = 0f; // 업데이트 간격 용

    public bool HasTarget => target != null;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();

        enemy = GetComponent<EnemyBase>();

        attack = GetComponent<IEnemyAttack>();

        if (enemy = null)
            Debug.LogError($"{name} : EnemyBase가 필요합니다");

        if (attack == null)
            Debug.LogWarning($"{name} : 컴포넌트가 없습니다.");

        if (target == null) //플레이어 자동 탐색
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                target = playerObj.transform;
        }

        if (enemy != null && agent != null) //EnemyBase의 moveSpeed를 navMeshAgent의 speed에도 반영
        {
            agent.speed = enemy.moveSpeed;
        }
    }

    private void Update()
    {
        if (enemy == null) return;

        if (enemy.IsDead)
        {
            ChangeState(AIState.Dead);
        }

        switch (state) //현재 상태에 따른 다른 Update로직 실행
        {
            case AIState.Idle:
                UpdateIdle();
                break;
            case AIState.Chase:
                UpdateChase();
                break;
            case AIState.Attack:
                UpdateAttack();
                break;
            case AIState.Dead:
                UpdateDead();
                break;
        }
    }

    private void ChangeState(AIState newState)
    {
        if (state == newState) return;

        state = newState;
        stateTimer = 0f;

        switch (state)
        {
            case AIState.Idle:
                agent.isStopped = true;   // 제자리 대기
                break;
            case AIState.Chase:
                agent.isStopped = false;  // 추적 시작
                break;
            case AIState.Attack:
                agent.isStopped = true;   // 공격할 땐 제자리
                break;
            case AIState.Dead:
                agent.isStopped = true;   // 죽으면 멈춤
                break;
        }
    }

    #region  각 상태별 Update 로직

    private void UpdateIdle()
    {
        stateTimer += Time.deltaTime;

        if (!HasTarget) //타겟 없으면 새로 검색
        {
            TryFindTarget();
            return;
        }

        if (stateTimer >= idleUpdateInterval)
        {
            stateTimer = 0f;

            float dist = DistanceToTarget();

            if (dist <= enemy.detectRange) //감지 범위 안에 들어오면 추적 시작
            {
                ChangeState(AIState.Chase);
            }
        }
    }

    private void UpdateChase() //chase = 추적 즉 타겟을 NavMesh로 따라감
    {
        if (!HasTarget)
        {
            ChangeState(AIState.Idle);
            return;
        }

        stateTimer += Time.deltaTime;

        float dist = DistanceToTarget();

        if (dist > loseTargetDistance)
        {
            ChangeState(AIState.Idle);
            return;
        }

        //공격 범위에 들어왔으면 Attack 상태로
        float attackRange = attack != null ? attack.AttackRange : enemy.attackRange;
        if (dist <= attackRange)
        {
            ChangeState(AIState.Attack);
            return;
        }

        //일정 주기로 목지만 갱신, 성능 최적화 목적으로 일단 넣어봄
        if (stateTimer >= chaseUpdateInterval)
        {
            stateTimer = 0f;
            agent.SetDestination(target.position);
        }
    }

    private void UpdateAttack() // attack 상태 
    {
        if (!HasTarget)
        {
            ChangeState(AIState.Idle);
            return;
        }

        float dist = DistanceToTarget();
        float attackRange = attack != null ? attack.AttackRange : enemy.attackRange;

        //너무 멀어졌으면 다시 추적 시작
        if (dist > attackRange * 1.2f)
        {
            ChangeState(AIState.Chase);
            return;
        }

        //타겟 쪽으로 회전
        Vector3 dir = (target.position - transform.position);
        dir.y = 0f;

        if (dir.sqrMagnitude > 0.01f)
        {
            Quaternion lookRot = Quaternion.LookRotation(dir.normalized);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, 10f * Time.deltaTime);
        }

        //공격 시도
        if (attack != null)
        {
            attack.TryAttack(target);
        }
        else
        {
            ChangeState(AIState.Chase);
        }
    }

    private void UpdateDead()
    {
        //사망 애니메이션 재생 후 Destory 실행
    }

    #endregion

    private float DistanceToTarget()
    {
        if (!HasTarget) return Mathf.Infinity;
        return Vector3.Distance(transform.position, target.position);
    }

    private void TryFindTarget()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            target = playerObj.transform;
    }
}
