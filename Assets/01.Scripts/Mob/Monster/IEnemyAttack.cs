using UnityEngine;

//몬스터 공격 공통 인터페이스

public interface IEnemyAttack
{
    bool IsAttacking { get; }

    void TryAttack(Transform target);
}
