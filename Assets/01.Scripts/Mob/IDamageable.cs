
public interface IDamageable
{
    void TakeDamage(DamageInfo info);
    bool IsAlive { get; }
}
// 데미지를 받을 수 있는 대상 모두 쓸 수 있는 공통 인터페이스
// 플레이어, 몬스터 등 모두 사용 가능
