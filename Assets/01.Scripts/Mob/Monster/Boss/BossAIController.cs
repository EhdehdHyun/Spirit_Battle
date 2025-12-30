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

    [Header("회전 설정")]
    [Tooltip("추적(Chase) 중 회전 속도")]
    [SerializeField] private float rotateSpeedChase = 8f;

    [Tooltip("공격(BasicAttack) 중 회전 속도 (0이면 공격 중 회전 안 함)")]
    [SerializeField] private float rotateSpeedAttack = 12f;

    [Tooltip("Slerp에 곱해질 보정값(너무 빠르면 낮추기)")]
    [SerializeField] private float rotateMultiplier = 1f;

    [Header("패턴 시스템 ")]
    public BossPatternBase[] patterns;

    private BossPatternBase currentPattern;
    private BossEnemy boss;
    private Transform target;
    private IEnemyAttack basicAttack;
    private EnemyMeleeAttack meleeAttack;
    private MonsterAnimation monsterAnim;

    private bool canMove = true;
    private bool uiLinked = false;

    private BossState state = BossState.Idle;
    private float stateTimer = 0f;

    public bool HasTarget => target != null && boss != null && !boss.IsDead;
    private Coroutine downCo;

    private void Awake()
    {
        boss = GetComponent<BossEnemy>();
        basicAttack = GetComponent<IEnemyAttack>();
        meleeAttack = GetComponent<EnemyMeleeAttack>();
        monsterAnim = GetComponent<MonsterAnimation>() ?? GetComponentInChildren<MonsterAnimation>();

        if (boss == null)
            Debug.LogError("BossEnemy 컴포넌트가 필요합니다");
        if (basicAttack == null)
            Debug.LogWarning("IEnemyAttack 구현체가 없습니다");

        if (target == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) target = playerObj.transform;
        }

        if (HasTarget)
            ChangeState(BossState.Chase);
    }

    private void Update()
    {
        if (boss == null) return;

        if (boss.IsDead) ChangeState(BossState.Dead);

        switch (state)
        {
            case BossState.Idle: UpdateIdle(); break;
            case BossState.Chase: UpdateChase(); break;
            case BossState.BasicAttack: UpdateBasicAttack(); break;
            case BossState.Pattern: UpdatePattern(); break;
        }

        UpdateAnimation();
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
            case BossState.BasicAttack:
            case BossState.Pattern:
            case BossState.Down:
                SetCanMove(false);
                break;

            case BossState.Chase:
                SetCanMove(true);
                break;
        }
    }

    private void UpdateIdle()
    {
        if (HasTarget) ChangeState(BossState.Chase);
    }

    private void UpdateChase()
    {
        if (!HasTarget)
        {
            ChangeState(BossState.Idle);
            return;
        }

        stateTimer += Time.deltaTime;

        if (TryUsePattern()) return;

        float atkRange = GetBasicAttackRange();
        float distAtk = DistToTargetFromAttackOrigin();

        if (distAtk <= atkRange)
        {
            ChangeState(BossState.BasicAttack);
            return;
        }

        float adjustedStop = Mathf.Min(stopDistance, Mathf.Max(0.1f, atkRange * 0.9f));
        MoveTowardsTarget(adjustedStop);
    }

    private void UpdateBasicAttack()
    {
        if (!HasTarget)
        {
            ChangeState(BossState.Idle);
            return;
        }

        float atkRange = GetBasicAttackRange();
        float distAtk = DistToTargetFromAttackOrigin();

        bool isAttacking = (basicAttack != null && basicAttack.IsAttacking);

        if (!isAttacking && distAtk > atkRange * 1.2f)
        {
            ChangeState(BossState.Chase);
            return;
        }

        if (!isAttacking && TryUsePattern()) return;

        // 공격 중에도 플레이어 쪽으로 돌아보게 하고 싶으면(선택)
        RotateTowardsTarget(rotateSpeedAttack);

        basicAttack?.TryAttack(target);
    }

    private void UpdatePattern()
    {
        if (currentPattern != null && currentPattern.IsRunning) return;

        currentPattern = null;

        if (!HasTarget)
        {
            ChangeState(BossState.Idle);
            return;
        }

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
        {
            anim.ResetTrigger(triggerName);
            anim.SetTrigger(triggerName);
        }

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

        RotateTowardsTarget(rotateSpeedChase);
    }

    private void RotateTowardsTarget(float speed)
    {
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
        if (state == BossState.Chase && canMove) speed = boss.moveSpeed;

        bool isChasing = (state == BossState.Chase);
        bool isDead = boss.IsDead;

        monsterAnim.UpdateLocomotion(speed, isChasing, isDead);
    }

    public void SetCanMove(bool value) => canMove = value;
}
