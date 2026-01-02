using UnityEngine;

public interface IParryReceiver
{
    // 공격이 들어왔을 때 지금 상태로 패링 성공 처리할지
    bool TryParry(WeaponHitBox hitBox, Vector3 hitPoint);
}

public interface IParryable
{
    // 몬스터가 패링 당했을 때 처리(스턴/경직/공격캔슬 등)
    void OnParried(ParryInfo info);
}

public struct ParryInfo
{
    public GameObject defender;
    public Vector3 point;
    public float stunTime;
}
