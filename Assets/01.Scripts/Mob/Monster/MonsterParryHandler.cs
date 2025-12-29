using UnityEngine;

public class MonsterParryHandler : MonoBehaviour, IParryable, IParryReceiver
{
    [Header("패링 윈도우")]
    [SerializeField] private bool parryWindowOpen = false;

    [Header("패링 그로기")]
    [SerializeField] private float defaultParryGroggyDuration = 2f;
    [SerializeField] private string parryGroggyTriggerName = "ParryGroggy";
    [SerializeField] private bool ignoreParryWhileGroggy = true;

    private CharacterBase character;
    private IParryGroggyController groggy;

    private void Awake() => CacheRefs();
    private void OnEnable() => CacheRefs();

    private void CacheRefs()
    {
        if (character == null) character = GetComponentInParent<CharacterBase>();
        if (groggy == null) groggy = GetComponentInParent<IParryGroggyController>();
    }

    public bool TryParry(WeaponHitBox hitBox, Vector3 hitPoint)
    {
        CacheRefs();

        if (!parryWindowOpen) return false;
        if (character != null && !character.IsAlive) return false;

        if (groggy == null)
        {
            Debug.LogWarning($"[{name}] IParryGroggyController를 부모에서 찾지 못함. (EnemyAIController/BossAIController에 인터페이스 구현 필요)", this);
            return false;
        }

        if (ignoreParryWhileGroggy && groggy.IsParryImmune)
            return false;

        // ✅ 패링 정보 만들고 공통 처리로 넘김
        ParryInfo info = new ParryInfo
        {
            defender = hitBox != null ? hitBox.gameObject : null,
            point = hitPoint,
            stunTime = defaultParryGroggyDuration
        };

        OnParried(info);
        return true;
    }

    public void OnParried(ParryInfo info)
    {
        CacheRefs();
        if (groggy == null) return;

        float duration = (info.stunTime > 0f) ? info.stunTime : defaultParryGroggyDuration;
        groggy.EnterParryGroggy(duration, parryGroggyTriggerName);

        parryWindowOpen = false;
    }

    // 애니메이션 이벤트
    public void Anim_ParryWindowOn() => parryWindowOpen = true;
    public void Anim_ParryWindowOff() => parryWindowOpen = false;
}
