using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossAIController : MonoBehaviour
{
    public enum BossState
    {
        Idle,
        Chase,
        BasicAttack,
        Pattern,
        Dead
    }

    [Header("타겟/전투 범위 설정")]
    [Tooltip("보스가 플레이어를 인식하고 전투를 시작하는 거리")]
    public float engageRange = 15f;

    [Tooltip("이 거리보다 멀어지면 타겟")]
    public float loseTargetDistance = 40f;

    [Header("이동 설정")]
    [Tooltip("플레이어에게 이 거리까지는 접근, 그 안에서는 멈춤")]
    public float stopDistance = 3f;

    [Tooltip("보스 회전 속도")]
    public float rotateSpeed = 8f;

    [Header("패턴 시스템 (추후 확장용)")]
    [Tooltip("패턴 시스템인데 추후에 패턴 로직 넣을 때 사용 예정")]
    public BossPatteernBase[] patterns;

    private BossEnemy boss; //보스 스탯/상태
    private Transform target; //플레이어
    private IEnemyAttack basicAttack; //기본 공격
    private MonsterAnimation monsterAnim;

    private BossState state = BossState.Idle;
    private float stateTimer = 0f;

    public bool HasTarget => target != null && !boss.IsDead;

    private void Awake()
    {
        boss = GetComponent<BossEnemy>();
        basicAttack = GetComponent<IEnemyAttack>();
        monsterAnim = GetComponent<MonsterAnimation>() ?? GetComponentInChildren<MonsterAnimation>();

        if (boss == null)
            Debug.LogError("BossEnemy 컴포넌트가 필요합니다");
        if (basicAttack == null)
            Debug.LogWarning("IEnemyAttack 구현체가 없습니다");

        if (target == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Play");
            if (playerObj != null)
                target = playerObj.transform;
        }
    }

    private void Update()
    {
        if (boss == null) return;

        if (boss.IsDead)
        {
            ChangeState(BossState.Dead);
        }

        switch (state)
        {
            case BossState.Idle:
                UpdateIdle();
                break;
            case BossState.Chase:
                UpdateChase();
                break;
            case BossState.BasicAttack:
                UpdateBasicAttack();
                break;
            case BossState.Pattern:
                UpdatePattern();
                break;
            case BossState.Dead:
                UpdateDead();
                break;
        }

        UpdateAnimation();
    }

    private void ChangeState(BossState newState)
    {
        if (state == newState) return;

        state = newState;
        stateTimer = 0f;

        switch (state)
        {
            case BossState.Idle:
                break;
            case BossState.Chase:
                break;
            case BossState.BasicAttack:
                break;
            case BossState.Pattern:
                //패턴 코루틴 시작 관련은 여기나 UpdatePattern에서 처리 
                break;
            case BossState.Dead:
                monsterAnim?.PlayDie();
                break;
        }
    }

    private void UpdateIdle()
    {
        stateTimer += Time.deltaTime;

        if (!HasTarget) return;

        float dist = DistanceToTarget();

        //사거리 안에 들어오면 추적 시작(시작 연출 필요하면 넣으면 될 거 같음)
        if (dist <= engageRange)
        {
            ChangeState(BossState.Chase);
        }
    }

    private void UpdateChase()
    {
        if (!HasTarget)
        {
            ChangeState(BossState.Idle);
            return;
        }

        stateTimer += Time.deltaTime;

        float dist = DistanceToTarget();

        if (dist > loseTargetDistance) //너무 멀어지면 어떻게 할 지
        {
            ChangeState(BossState.Idle);
            return;
        }

        float attackRange = boss.attackRange;
        if (dist <= attackRange)
        {
            ChangeState(BossState.BasicAttack);
            return;
        }

        MoveTowardsTarget(stopDistance);
    }

    //기본 근접 공격 상태
    private void UpdateBasicAttack()
    {
        if (!HasTarget)
        {
            ChangeState(BossState.Idle);
            return;
        }

        float dist = DistanceToTarget();
        float attackRange = boss.attackRange;

        bool isAttacking = (basicAttack != null && basicAttack.IsAttacking);

        // 공격 중이 아니고, 너무 멀어졌으면 다시 추적
        if (!isAttacking && dist > attackRange * 1.2f)
        {
            ChangeState(BossState.Chase);
            return;
        }

        // 제자리에서 플레이어를 계속 바라보게
        LookAtTarget();

        // 기본 공격 시도 (쿨타임/거리 체크는 EnemyMeleeAttack 안에서 처리)
        basicAttack?.TryAttack(target);

        // 나중에 여기에서 일정 조건/쿨타임마다 패턴으로 전환하도록 예정
    }

    //패턴 상태 나중에 패턴 스크립트 작성 후 여기에 연결하면 됨
    private void UpdatePattern()
    {
        ChangeState(BossState.Chase);
    }

    private void UpdateDead()
    {
        //사망 상태 : 사망 애니메이션 넣을 예정
    }

    private float DistanceToTarget()
    {
        if (!HasTarget) return Mathf.Infinity;
        return Vector3.Distance(transform.position, target.position);
    }

    private void MoveTowardsTarget(float stopDistance)
    {
        if (!HasTarget) return;

        Vector3 toTarget = target.position - transform.position;
        toTarget.y = 0f;

        float dist = toTarget.magnitude;
        if (dist <= stopDistance) return;

        Vector3 dir = toTarget.normalized;

        transform.position += dir * boss.moveSpeed * Time.deltaTime;

        if (dir.sqrMagnitude > 0.001f)
        {
            Quaternion lookRot = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, rotateSpeed * Time.deltaTime);
        }
    }

    private void LookAtTarget()
    {
        if (!HasTarget) return;

        Vector3 dir = target.position - transform.position;
        dir.y = 0f;

        if (dir.sqrMagnitude > 0.001f)
        {
            Quaternion lookRot = Quaternion.LookRotation(dir.normalized);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, rotateSpeed * Time.deltaTime);
        }
    }

    private void UpdateAnimation()
    {
        if (monsterAnim == null) return;

        float speed = 0f;

        if (state == BossState.Chase)
        {
            speed = boss.moveSpeed;
        }

        bool isChasing = (state == BossState.Chase);
        bool isDead = boss.IsDead;

        monsterAnim.UpdateLocomotion(speed, isChasing, isDead);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, engageRange);

        Gizmos.color = Color.red;
        if (boss != null)
            Gizmos.DrawWireSphere(transform.position, boss.attackRange);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, loseTargetDistance);
    }
}
