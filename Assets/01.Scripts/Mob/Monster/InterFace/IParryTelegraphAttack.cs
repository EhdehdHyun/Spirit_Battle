using UnityEngine;

public interface IParryTelegraphAttack
{
    bool Parryable { get; }
    ParryTelegraphType TelegraphType { get; }
    float ParryWindowDuration { get; } // 0.2~0.4
    float ParryStunTime { get; }       // 패링 성공시 보스 스턴 시간
}


