using UnityEngine;

public struct DamageInfo
{
    public float amount; //데미지량
    public Vector3 hitPoint; //맞은 위치 (이펙트 넣으면 이펙트가 나올 곳)
    public Vector3 hitNormal; //맞은 방향 (넉백용 )

    public DamageInfo(float amount, Vector3 hitPoint, Vector3 hitNormal)
    {
        this.amount = amount;
        this.hitPoint = hitPoint;
        this.hitNormal = hitNormal;
    }
}
