using UnityEngine;

public class BossEnemy : EnemyBase
{
    [Header("보스 페이즈 설정")]
    [Tooltip("보스의 최대 페이즈 수 (ex. 1, 2, 3)")]
    public int maxPhase = 1;
    [Tooltip("다음 페이즈로 넘어가는 HP 비율 (0~1 ex.0.5 = 체력 절반 이하에서 2페이즈")]
    public float phase2HpRatio = 0.5f;

    public int CurrentPhase { get; private set; } = 1;

    protected override void OnDamaged(DamageInfo info)
    {
        base.OnDamaged(info);

        //페이즈 전환 1 -> 2 
        if (maxPhase >= 2 && CurrentPhase == 1)
        {
            float hpRatio = currentHp / maxHp;
            if (CurrentPhase <= phase2HpRatio)
            {
                CurrentPhase = 2;
                OnPhaseChanged(CurrentPhase);
            }
        }
    }

    protected virtual void OnPhaseChanged(int newPhase)
    {
        //페이즈 전환 연출, 스탯 변경, 새로운 패턴 개방 등등 들어갈 메써드
    }

    protected override void OnDie(DamageInfo info)
    {
        base.OnDie(info);
    }
}
