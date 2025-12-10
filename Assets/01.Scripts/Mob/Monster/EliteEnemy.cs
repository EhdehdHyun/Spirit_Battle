using UnityEngine;

public class EliteEnemy : EnemyBase
{
    [Header("엘리트 추가 설정")]
    [Tooltip("데미지 배율 (Normal 몬스터의 ")]
    public float damageMultiplier = 1.5f;

    protected override void Awake()
    {
        base.Awake();
        maxHp *= 2f;
        currentHp = maxHp;
    }
}
