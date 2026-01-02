using UnityEngine;

public class BossParryGroggyAdapter : MonoBehaviour, IParryGroggyController
{
    [SerializeField] private BossAIController bossAI;
    [SerializeField] private BossEnemy bossEnemy;

    private void Awake()
    {
        if (bossAI == null) bossAI = GetComponentInChildren<BossAIController>();
        if (bossEnemy == null) bossEnemy = GetComponentInChildren<BossEnemy>();
    }

    public bool IsParryImmune
    {
        get
        {
            if (bossEnemy != null && bossEnemy.IsDead) return true;

            if (bossAI != null && bossAI.IsDownState) return true;

            return false;
        }
    }

    public void EnterParryGroggy(float duration, string triggerName)
    {
        if (bossAI == null) return;
        bossAI.EnterParryGroggy(duration, triggerName);
    }
}
