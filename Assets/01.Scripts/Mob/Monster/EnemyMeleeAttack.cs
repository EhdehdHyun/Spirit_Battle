using UnityEngine;

public class EnemyMeleeAttack : MonoBehaviour, IEnemyAttack
{
    [Header("공격 설정")]
    public float damage = 10f;
    public float attackCooldown = 1.5f;

    [Header("히트박스 설정")]
    public Transform hitOrigin;
    public float hitRadius = 1f;
    public LayerMask hitMask;

    [Header("Phase2 독 상태이상 (추후 구현 예정)")]
    public float posionDamagePerSecond = 5f;
    public float posionDuration = 3f;

    private EnemyBase enemy;
    private Animator animator;
    private float lastAttackTime = -999f;
    private bool isAttacking = false;

    public bool IsAttacking => isAttacking;

    // AI가 공격 기준을 가져갈 수 있게 제공
    public Transform AttackOrigin => (hitOrigin != null) ? hitOrigin : transform;
    public float AttackRadius => hitRadius;

    private void Awake()
    {
        enemy = GetComponent<EnemyBase>();
        animator = GetComponentInChildren<Animator>();

        if (animator == null)
            Debug.LogWarning("Animator를 찾지 못했습니다.");

        if (hitOrigin == null)
            hitOrigin = transform; // fallback
    }

    public void TryAttack(Transform target)
    {
        if (target == null) return;

        if (Time.time - lastAttackTime < attackCooldown) return;

        Vector3 center = AttackOrigin.position;
        float dist = Vector3.Distance(center, target.position);

        if (dist > hitRadius) return;

        lastAttackTime = Time.time;
        isAttacking = true;

        if (animator != null) animator.SetTrigger("Attack");
        else PerformHit();
    }

    public void PerformHit()
    {
        Collider[] cols = Physics.OverlapSphere(AttackOrigin.position, hitRadius, hitMask);

        foreach (Collider col in cols)
        {
            IDamageable damageable = col.GetComponentInParent<IDamageable>();
            if (damageable != null)
            {
                Vector3 hitPoint = col.ClosestPoint(AttackOrigin.position);
                Vector3 hitNormal = (hitPoint - AttackOrigin.position).normalized;

                DamageInfo info = new DamageInfo(damage, hitPoint, hitNormal);
                damageable.TakeDamage(info);

                if (enemy is BossEnemy bossEnemy && bossEnemy.CurrentPhase >= 2)
                {
                    // TODO: 상태이상
                }
            }
        }
    }

    public void OnAttackHit() => PerformHit();
    public void OnAttackEnd() => isAttacking = false;


    private void OnDrawGizmosSelected()
    {
        if (hitOrigin == null) return;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(hitOrigin.position, hitRadius);
    }
}
