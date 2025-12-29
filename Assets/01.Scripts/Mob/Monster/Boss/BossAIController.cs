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
    public float stopDistance = 3f;
    public float rotateSpeed = 8f;

    [Header("패턴 시스템 ")]
    public BossPatternBase[] patterns;

    private BossPatternBase currentPattern;
    private BossEnemy boss;
    private BossCore bossCore;
    private Transform target;
    private IEnemyAttack basicAttack;
    private EnemyMeleeAttack meleeAttack; // ✅ 추가(보스도 EnemyMeleeAttack 쓰는 전제)
    private MonsterAnimation monsterAnim;

    private bool canMove = true;
    private bool uiLinked = false;

    private BossState state = BossState.Idle;
    private float stateTimer = 0f;

    public bool HasTarget => target != null && !boss.IsDead;
    private Coroutine downCo;

    // ✅ 공격 기준: hitOrigin/hitRadius 우선
    private Transform AttackOrigin => (meleeAttack != null) ? meleeAttack.AttackOrigin : transform;
    private float AttackRange => (meleeAttack != null) ? meleeAttack.AttackRadius : boss.attackRange;

    private void Awake()
    {
        boss = GetComponent<BossEnemy>();
        basicAttack = GetComponent<IEnemyAttack>();
        meleeAttack = GetComponent<EnemyMeleeAttack>(); // ✅ 추가
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

        if (HasTarget)
            ChangeState(BossState.Chase);
    }

    private void Update()
    {
        if (boss == null) return;

        if (boss.IsDead)
            ChangeState(BossState.Dead);

        switch (state)
        {
            case BossState.Idle: UpdateIdle(); break;
            case BossState.Chase: UpdateChase(); break;
            case BossState.BasicAttack: UpdateBasicAttack(); break;
            case BossState.Pattern: UpdatePattern(); break;
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
            case BossState.Idle: SetCanMove(false); break;
            case BossState.BasicAttack: SetCanMove(false); break;
            case BossState.Chase: SetCanMove(true); break;
            case BossState.Pattern: SetCanMove(false); break;
            case BossState.Down: SetCanMove(false); break;
        }
    }

    private void UpdateIdle()
    {
        if (HasTarget)
            ChangeState(BossState.Chase);
    }

    private void UpdateChase()
    {
        if (!HasTarget)
        {
            ChangeState(BossState.Idle);
            return;
        }

        // 패턴 먼저
        if (TryUsePattern()) return;

        // ✅ 공격 전환 판단을 hitOrigin/hitRadius로
        float distToAttackOrigin = Vector3.Distance(AttackOrigin.position, target.position);
        if (distToAttackOrigin <= AttackRange)
        {
            ChangeState(BossState.BasicAttack);
            return;
        }

        MoveTowardsTarget(stopDistance);
    }

    private void UpdateBasicAttack()
    {
        if (!HasTarget)
        {
            ChangeState(BossState.Idle);
            return;
        }

        bool isAttacking = (basicAttack != null && basicAttack.IsAttacking);

        // ✅ “너무 멀어졌나?”도 hitOrigin 기준
        float distToAttackOrigin = Vector3.Distance(AttackOrigin.position, target.position);

        if (!isAttacking && distToAttackOrigin > AttackRange * 1.2f)
        {
            ChangeState(BossState.Chase);
            return;
        }

        if (!isAttacking && TryUsePattern()) return;

        basicAttack?.TryAttack(target);
    }

    private void UpdatePattern()
    {
        if (currentPattern != null && currentPattern.IsRunning)
            return;

        currentPattern = null;

        if (!HasTarget)
        {
            ChangeState(BossState.Idle);
            return;
        }

        // ✅ 패턴 끝나고 상태 복귀 판단도 hitOrigin 기준
        float distToAttackOrigin = Vector3.Distance(AttackOrigin.position, target.position);

        if (distToAttackOrigin <= AttackRange)
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

        if (!HasTarget) ChangeState(BossState.Idle);
        else ChangeState(BossState.Chase);
    }

    public bool IsDownState => state == BossState.Down;

    public void EnterParryGroggy(float duration, string triggerName)
    {
        StopAllCoroutines();
        ChangeState(BossState.Down);

        var anim = GetComponentInChildren<Animator>();
        if (anim != null && !string.IsNullOrEmpty(triggerName))
        {
            anim.ResetTrigger(triggerName);
            anim.SetTrigger(triggerName);
        }

        downCo = StartCoroutine(DownTimer(duration));
    }

    private void MoveTowardsTarget(float stopDist)
    {
        if (!HasTarget) return;
        if (!canMove) return;

        Vector3 toTarget = target.position - transform.position;
        toTarget.y = 0f;

        float dist = toTarget.magnitude;
        if (dist <= stopDist) return;

        Vector3 dir = toTarget.normalized;

        transform.position += dir * boss.moveSpeed * Time.deltaTime;

        if (dir.sqrMagnitude > 0.001f)
        {
            Quaternion lookRot = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, rotateSpeed * Time.deltaTime);
        }
    }

    private void UpdateAnimation()
    {
        if (monsterAnim == null) return;

        float speed = 0f;
        if (state == BossState.Chase && canMove)
            speed = boss.moveSpeed;

        bool isChasing = (state == BossState.Chase);
        bool isDead = boss.IsDead;

        monsterAnim.UpdateLocomotion(speed, isChasing, isDead);
    }

    private void OnDrawGizmosSelected()
    {
        if (boss == null) boss = GetComponent<BossEnemy>();

        Gizmos.color = Color.red;
        Vector3 center = (meleeAttack != null && meleeAttack.AttackOrigin != null) ? meleeAttack.AttackOrigin.position : transform.position;
        float r = (meleeAttack != null) ? meleeAttack.AttackRadius : (boss != null ? boss.attackRange : 0f);
        if (r > 0f) Gizmos.DrawWireSphere(center, r);
    }

    public void SetCanMove(bool value) => canMove = value;
}
