using System.Collections;
using UnityEngine;

public abstract class BossPatternBase : MonoBehaviour
{
    [Header("공통 패턴 설정")]
    public string patterName = "Pattern";
    public float cooldown = 5f;

    [Tooltip("이 거리 안/밖에서만 사용하고 싶을 때(0이면 무시)")]
    public float minUseDistance = 0f;
    public float maxUseDistance = 0f;

    [Header("페이즈 조건")]
    [Tooltip("이상 페이즈에서만 사용")]
    public int minPhase = 1;

    [Tooltip("이하 페이즈에서만 사용")]
    public int maxPhase = 99;

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

        // if (bossAI != null)
        //     target = bossAI.transform;
    }

    public virtual bool CanExecute(Transform currentTarget)
    {
        if (isRunning) return false;
        if (Time.time - lastUseTime < cooldown) return false;
        if (boss == null) return false;

        if (boss is BossEnemy be)
        {
            if (be.CurrentPhase < minPhase || be.CurrentPhase > maxPhase)
                return false;
        }

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

        //패턴 동안 이동 잠그기
        if (bossAI != null)
            bossAI.SetCanMove(false);

        yield return ExecutePattern();

        //패턴 끝나면 다시 이동 허용
        if (bossAI != null)
            bossAI.SetCanMove(true);

        isRunning = false;
    }

    protected abstract IEnumerator ExecutePattern();
}
