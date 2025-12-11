using System.Collections;
using System.ComponentModel;
using UnityEngine;

public abstract class BossPatteernBase : MonoBehaviour
{
    [Header("공통 패턴 설정")]
    public string patterName = "Pattern";
    public float cooldown = 5f;

    [Tooltip("이 거리 안/밖에서만 사용하고 싶을 때(0이면 무시)")]
    public float minUseDistance = 0f;
    public float maxUseDistance = 0f;

    protected BossAIController bossAI;
    protected BossEnemy boss;
    protected Transform target;

    protected bool isRunning = false;
    protected float lastUseTime = -999f;

    public bool IsRunning => isRunning;

    protected virtual void Awake()
    {
        bossAI = GetComponentInChildren<BossAIController>();
        boss = GetComponentInParent<BossEnemy>();

        if (bossAI != null)
            target = bossAI.transform;
    }

    public virtual bool CanExecute(Transform currentTarget)
    {
        if (isRunning) return false;
        if (Time.time - lastUseTime < cooldown) return false;
        if (currentTarget == null) return false;

        if (boss == null) return false;

        float dist = Vector3.Distance(boss.transform.position, currentTarget.position);

        if (minUseDistance > 0f && dist < minUseDistance) return false;
        if (maxUseDistance > 0f && dist > maxUseDistance) return false;

        return true;
    }

    //패턴 실행용 코루틴 
    public IEnumerator Excute(Transform currentTarget)
    {
        if (isRunning) yield break;

        target = currentTarget;
        isRunning = true;
        lastUseTime = Time.time;

        yield return ExecutePattern();

        isRunning = false;
    }

    protected abstract IEnumerator ExecutePattern();
}
