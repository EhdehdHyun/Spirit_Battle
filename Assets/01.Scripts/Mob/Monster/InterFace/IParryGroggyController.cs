

public interface IParryGroggyController
{
    // 이미 그로기/다운/죽음 등으로 패링을 더 받으면 안 되는 상태면 true
    bool IsParryImmune { get; }

    // 패링 성공 시 호출
    void EnterParryGroggy(float duration, string triggerName);
}
