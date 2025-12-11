using UnityEngine;

public class BossEnemy : EnemyBase
{
    [Header("보스 페이즈 설정")]
    [Tooltip("보스의 최대 페이즈 수 (ex. 1, 2, 3)")]
    public int maxPhase = 1;
    [Tooltip("다음 페이즈로 넘어가는 HP 비율 (0~1 ex.0.5 = 체력 절반 이하에서 2페이즈")]
    public float phase2HpRatio = 0.5f;
}
