using UnityEngine;

public class MonsterParryHandler : MonoBehaviour, IParryable, IParryReceiver
{
    [Header("패링 윈도우")]
    [SerializeField] private bool parryWindowOpen = false;

    [Header("패링 그로기")]
    [SerializeField] private float defaultParryGroggyDuration = 2f;
    [SerializeField] private string parryGroggyTriggerName = "ParryGroggy";
    [SerializeField] private bool ignoreParryWhileGroggy = true;

    [Header("Parry Telegraph (Shrink Rings)")]
    [SerializeField] private ParryTelegraphShrinkRing telegraph;

    private CharacterBase character;
    private IParryGroggyController groggy;

    private void Awake() => CacheRefs();
    private void OnEnable() => CacheRefs();

    private void CacheRefs()
    {
        if (character == null) character = GetComponentInParent<CharacterBase>();
        if (groggy == null) groggy = GetComponentInParent<IParryGroggyController>();

        if (telegraph == null) telegraph = GetComponentInChildren<ParryTelegraphShrinkRing>(true);
    }

    public bool TryParry(WeaponHitBox hitBox, Vector3 hitPoint)
    {
        CacheRefs();

        if (!parryWindowOpen) return false;
        if (character != null && !character.IsAlive) return false;

        if (groggy == null)
        {
            Debug.LogWarning($"[{name}] IParryGroggyController를 부모에서 찾지 못함.", this);
            return false;
        }

        if (ignoreParryWhileGroggy && groggy.IsParryImmune)
            return false;

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
        Debug.Log($"[BOSS PARRY] Groggy to {groggy} duration={info.stunTime}", this);
        CacheRefs();
        if (groggy == null) return;

        float duration = (info.stunTime > 0f) ? info.stunTime : defaultParryGroggyDuration;
        groggy.EnterParryGroggy(duration, parryGroggyTriggerName);

        // 패링 성공 연출 즉시 원 제거
        telegraph?.ParrySuccessHide();

        // 타임 슬로우 
        ParryTimeSlow.Play();

        ParryPostFxPulse.Play();

        ParryPostFxPulse.Play();

        parryWindowOpen = false;

        QuestManager.Instance.ReportProgress(
            CompleteCondition.UseSkill,
            0,   // 패링은 TargetID 안 씀
            1
        );
    }

    // 공격 시작 시점
    public void Anim_TelegraphStart()
    {
        telegraph?.TelegraphStart();
    }

    // 패링 타이밍 ON
    public void Anim_ParryWindowOn()
    {
        parryWindowOpen = true;
        telegraph?.ParryWindowOn();
    }

    // 패링 타이밍 OFF
    public void Anim_ParryWindowOff()
    {
        parryWindowOpen = false;
        telegraph?.ParryWindowOff();
    }

    // 공격 애니 끝
    public void Anim_TelegraphEnd()
    {
        telegraph?.TelegraphEnd();
    }
}
