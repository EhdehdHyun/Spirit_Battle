using System.Collections;
using UnityEngine;

public class BossAIController : MonoBehaviour
{
    public enum BossState
    {
        Idle,
        Chase,
        BasicAttack,
        Pattern,
        Down,
        Dead
    }

    [Header("이동 설정")]
    [Tooltip("플레이어에게 이 거리까지는 접근, 그 안에서는 멈춤")]
    public float stopDistance = 3f;

    [Tooltip("보스 회전 속도")]
    public float rotateSpeed = 8f;

    [Header("패턴 시스템 ")]
    public BossPatternBase[] patterns;

    private BossPatternBase currentPattern;
    private BossEnemy boss; //보스 스탯/상태
    private BossCore bossCore;
    private Transform target; //플레이어
    private IEnemyAttack basicAttack; //기본 공격
    private MonsterAnimation monsterAnim;

    // 이동 잠금 여부
    private bool canMove = true;

    //UI 한 번만 연결하기 위한 플래그
    private bool uiLinked = false;

    private BossState state = BossState.Idle;
    private float stateTimer = 0f;

    public bool HasTarget => target != null && !boss.IsDead;
    private Coroutine downCo;

    private void Awake()
    {
        boss = GetComponent<BossEnemy>();
        basicAttack = GetComponent<IEnemyAttack>();
        monsterAnim = GetComponent<MonsterAnimation>() ?? GetComponentInChildren<MonsterAnimation>();
        bossCore = GetComponentInChildren<BossCore>();

        if (boss == null)
            Debug.LogError("BossEnemy 컴포넌트가 필요합니다");
        if (basicAttack == null)
            Debug.LogWarning("IEnemyAttack 구현체가 없습니다");

        if (target == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                target = playerObj.transform;
        }

        //어차피 보스전에서는 무조건 플레이를 쫓아오게 되어있으니까
        if (HasTarget)
            ChangeState(BossState.Chase);
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
            case BossState.Down:
                //UpdateDown();
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

        if (state == BossState.Chase && !uiLinked)
        {
            var ui = BossUIStatus.Instance;
            if (ui != null && boss != null)
            {
                ui.SetBoss(boss);
                uiLinked = true;
            }
        }

        switch (state)
        {
            case BossState.Idle:
                SetCanMove(false);
                break;

            case BossState.BasicAttack:
                SetCanMove(false);
                break;

            case BossState.Chase:
                SetCanMove(true);
                break;

            case BossState.Pattern:
                SetCanMove(false);
                break;
            case BossState.Down:
                SetCanMove(false);
                break;
            case BossState.Dead:
                SetCanMove(false);
                monsterAnim?.PlayDie();
                break;
        }
    }

    private void UpdateIdle()
    {
        if (HasTarget)
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

        //패턴 먼저 시도
        if (TryUsePattern()) return;

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

        if (!isAttacking && TryUsePattern()) return;

        // 기본 공격 시도 (쿨타임/거리 체크는 EnemyMeleeAttack 안에서 처리)
        basicAttack?.TryAttack(target);
    }

    private void UpdatePattern()
    {
        if (currentPattern != null && currentPattern.IsRunning)
        {
            return;
        }

        currentPattern = null;
        if (!HasTarget)
        {
            ChangeState(BossState.Idle);
            return;
        }

        float dist = DistanceToTarget();
        if (dist <= boss.attackRange)
            ChangeState(BossState.BasicAttack);
        else
            ChangeState(BossState.Chase);
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
            StartCoroutine(currentPattern.Excute(target));
            ChangeState(BossState.Pattern);
            return true;
        }

        return false;
    }

    public void HandleCoreBroken()
    {
        StartCoroutine(DownRoutine());
    }

    public void EnterBreakGroggy(float duration, string triggerName)
    {

        StopAllCoroutines();

        ChangeState(BossState.Down);

        var anim = GetComponentInChildren<Animator>();
        if (anim != null && !string.IsNullOrEmpty(triggerName))
            anim.SetTrigger(triggerName);

        downCo = StartCoroutine(DownTimer(duration));
    }

    private IEnumerator DownTimer(float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            yield return null;
        }

        // 다운 끝나면 다시 전투 재개
        if (!HasTarget) ChangeState(BossState.Idle);
        else ChangeState(BossState.Chase);
    }

    private IEnumerator DownRoutine()
    {
        ChangeState(BossState.Down);

        float downDuration = 5f; // 임시 값 
        float timer = 0f;
        while (timer < downDuration)
        {
            timer += Time.deltaTime;
            yield return null;
        }

        if (!HasTarget)
            ChangeState(BossState.Idle);
        else
        {
            float dist = DistanceToTarget();
            if (dist <= boss.attackRange)
                ChangeState(BossState.BasicAttack);
            else
                ChangeState(BossState.Chase);
        }
    }

    public bool IsDownState => state == BossState.Dead;
    public void EnterParryGroggy(float duration, string triggerName)
    {
        StopAllCoroutines();
        ChangeState(BossState.Down);

        var anim = GetComponentInChildren<Animator>();
        if (anim != null && !string.IsNullOrEmpty(triggerName))
        {
            anim.ResetTrigger(triggerName); // 트리거 씹힘 방지
            anim.SetTrigger(triggerName);
        }

        downCo = StartCoroutine(DownTimer(duration));
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
        if (!canMove) return;

        Vector3 toTarget = target.position - transform.position;
        toTarget.y = 0f;

        float dist = toTarget.magnitude;
        if (dist <= stopDistance) return;

        Vector3 dir = toTarget.normalized;

        transform.position += dir * boss.moveSpeed * Time.deltaTime;

        if (dir.sqrMagnitude > 0.001f)
        {
            Quaternion lookRot = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.Slerp(
                transform.rotation, lookRot, rotateSpeed * Time.deltaTime);
        }
    }

    private void UpdateAnimation()
    {
        if (monsterAnim == null) return;

        float speed = 0f;

        if (state == BossState.Chase && canMove)
        {
            speed = boss.moveSpeed;
        }

        bool isChasing = (state == BossState.Chase);
        bool isDead = boss.IsDead;

        monsterAnim.UpdateLocomotion(speed, isChasing, isDead);
    }

    private void OnDrawGizmosSelected()
    {
        if (boss == null)
            boss = GetComponent<BossEnemy>();

        // 공격 범위 정도만 참고용으로 표시
        Gizmos.color = Color.red;
        if (boss != null)
            Gizmos.DrawWireSphere(transform.position, boss.attackRange);
    }

    public void SetCanMove(bool value)
    {
        canMove = value;
    }
}
