using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyMeleeAttack : MonoBehaviour, IEnemyAttack
{
    [Header("공격 설정")]
    public float damage = 10f; //공격 시 입히는 피해량
    public float attackCooldown = 1.5f;

    [Header("히트박스 설정")]
    public Transform hitOrigin; //공격 판정 기준 위치 
    public float hitRadius = 1f; // 공격 판정 반지름
    public LayerMask hitMask; //플레이어 판정 레이어

    private EnemyBase enemy;
    private Animator animator;
    private float lastAttackTime = -999f;
    private bool isAttacking = false;

    public bool IsAttacking => isAttacking;

    private void Awake()
    {
        enemy = GetComponent<EnemyBase>();
        animator = GetComponentInChildren<Animator>();

        if (animator == null)
        {
            Debug.LogWarning("Animator를 찾지 못했습니다.");
        }

        if (hitOrigin == null)
        {
            hitOrigin = transform;
        }
    }
    public void TryAttack(Transform target)
    {
        if (target == null) return;

        //if (isAttacking) return; //만약 이미 공격 중이면 새로운 공격 시작 못 하게

        //쿨타임 체크
        if (Time.time - lastAttackTime < attackCooldown) return;

        //거리 체크
        float dist = Vector3.Distance(transform.position, target.position);
        float range = enemy != null ? enemy.attackRange : 2f;
        if (dist > range) return;


        //공격 시작
        lastAttackTime = Time.time;
        isAttacking = true;

        if (animator != null)
        {
            animator.SetTrigger("Attack");
        }
        else
        {
            PerformHit();
        }
    }

    public void PerformHit()
    {
        //hitOrigin의 위치 기준으로 , hitRadius의 반지름 만큼의 동그란 범위를 그림, 그 범위 속 콜라이더를 가져옴
        Collider[] cols = Physics.OverlapSphere(hitOrigin.position, hitRadius, hitMask);

        foreach (Collider col in cols)
        {
            IDamageable damageable = col.GetComponentInParent<IDamageable>();
            //IDamageable이 있는지 즉 데미지를 받을 수 있는지 확인
            if (damageable != null)
            {
                //Damageinfo(타격 위치, 방향) 생성
                Vector3 hitPoint = col.ClosestPoint(hitOrigin.position);
                Vector3 hitNormal = (hitPoint - hitOrigin.position).normalized;

                DamageInfo info = new DamageInfo(damage, hitPoint, hitNormal);
                damageable.TakeDamage(info);
            }
        }
    }

    //공격 모션 중간에 애니메이션 이벤트로 호출 
    public void OnAttackHit()
    {
        PerformHit();
    }

    //공격 애니메이션 끝나는 타이밍에 애니메이션 이벤트로 호출
    public void OnAttackEnd()
    {
        isAttacking = false;
    }

    private void OnDrawGizmosSelected()
    {
        if (hitOrigin == null) return;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(hitOrigin.position, hitRadius);
    }
}
