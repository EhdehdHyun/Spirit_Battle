using Unity.VisualScripting;
using UnityEditor.EditorTools;
using UnityEngine;

public class MonsterParryHandler : MonoBehaviour, IParryable, IParryReceiver
{
    [Header("패링 윈도우")]
    [Tooltip("애니메이션 이벤트로 켜고 끌 패링 가능 구간")]
    [SerializeField] private bool parryWindowOpen = false;

    [Header("패링 그로기")]
    [SerializeField] private float defaultParryGroggyDuration = 2f;
    [SerializeField] private string parryGroggyTriggerName = "ParryGroggy";
    [Tooltip("브레이크 그로기 중엔 패링을 무시할지(혹시 모를 상황을 대비해 체크용으로 넣어 둠)")]
    [SerializeField] private bool ignoreParryWhileBreakGroggy = true;

    private BossAIController bossAI;
    private BossEnemy bossEnemy;

    private void Awake()
    {
        bossAI = GetComponentInParent<BossAIController>();
        bossEnemy = GetComponentInParent<BossEnemy>();
    }

    public bool TryParry(WeaponHitBox hitBox, Vector3 hitPoint)
    {
        if (bossEnemy != null && bossEnemy.IsDead) return false;

        if (ignoreParryWhileBreakGroggy && bossAI != null && bossAI.IsDownState)
            return false;

        if (!parryWindowOpen) return false;

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
        if (bossAI == null) return;
        float duration = (info.stunTime > 0f) ? info.stunTime : defaultParryGroggyDuration;

        bossAI.EnterParryGroggy(duration, parryGroggyTriggerName);

        parryWindowOpen = false;


    }

    //애니메이션 이벤트용(공격 모션 중간 패링이 가능한 구간에 애니메이션 삽입)
    public void Anim_ParryWindowOn() => parryWindowOpen = true;
    public void Anim_ParryWindowOff() => parryWindowOpen = false;
}
