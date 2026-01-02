using UnityEngine;

//몬스터 공격 공통 인터페이스
//근/원거리 상관 없이 일단 EnermyAiController가 이 인터페이스만 보고 호출

public interface IEnemyAttack
{
    bool IsAttacking { get; }

    void TryAttack(Transform target);
    //타겟을 향해 공격 시도
    //쿨타임,거리 체크는 구현체 내에서 처리
    //쉽게 AttackRange를 통해 거리를 확인하면 일단 TryAttack을 호출
}
