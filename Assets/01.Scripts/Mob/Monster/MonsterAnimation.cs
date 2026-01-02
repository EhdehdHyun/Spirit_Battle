using UnityEngine;

public class MonsterAnimation : MonoBehaviour
{
    [Header("애니메이터")]
    [SerializeField] private Animator animator;

    [Header("파라미터 이름 설정")] //애니메이터에서 각각
    [SerializeField] private string moveSpeedParm = "MoveSpeed"; //float
    [SerializeField] private string isRunningParm = "IsRunning"; //bool
    [SerializeField] private string dieTriggerParm = "Die"; //triger

    [Header("피격 트리거(일반/엘리트용)")]
    [SerializeField] private string hitTriggerParm = "Hit";

    [Header("상태 태그")]
    [SerializeField] private int layerIndex = 0;
    [SerializeField] private string attackTag = "Attack";
    [SerializeField] private string hitTag = "Hit";

    private int moveSpeedHash;
    private int isRunningHash;
    private int dieTriggerHash;
    private int hitTriggerHash;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (animator == null)
        {
            Debug.LogWarning("Animator를 찾지 못했습니다. 애니메이션이 실행이 안 될 수 있습니다.");
            return;
        }

        moveSpeedHash = Animator.StringToHash(moveSpeedParm);
        isRunningHash = Animator.StringToHash(isRunningParm);
        dieTriggerHash = Animator.StringToHash(dieTriggerParm);
        hitTriggerHash = Animator.StringToHash(hitTriggerParm);
    }

    private bool IsTagPlaying(string tag)
    {
        if (animator == null) return false;

        var cur = animator.GetCurrentAnimatorStateInfo(layerIndex);
        bool inTrans = animator.IsInTransition(layerIndex);
        if (!inTrans) return cur.IsTag(tag);

        var next = animator.GetNextAnimatorStateInfo(layerIndex);
        return cur.IsTag(tag) || next.IsTag(tag);

    }

    public bool IsInAttack => IsTagPlaying(attackTag);
    public bool IsInHit => IsTagPlaying(hitTag);

    public bool CanPlayHit()
    {
        if (IsInAttack) return false;
        if (IsInHit) return false;

        return true;
    }

    public bool TryPlayHit()
    {
        if (animator == null) return false;
        if (!CanPlayHit()) return false;

        animator.ResetTrigger(hitTriggerHash);
        animator.SetTrigger(hitTriggerHash);
        return true;
    }

    //이동 관련 애니메이션 업데이트
    public void UpdateLocomotion(float speed, bool isChasing, bool isDead)
    {
        if (animator == null) return;

        if (isDead)
        {
            animator.SetFloat(moveSpeedHash, 0f);
            animator.SetBool(isRunningHash, false);
            return;
        }

        animator.SetFloat(moveSpeedHash, speed);

        bool running = isChasing && speed > 0.1f;
        animator.SetBool(isRunningHash, running);
    }

    //사망 애니메이션 트리거
    public void PlayDie()
    {
        if (animator == null) return;
        if (!string.IsNullOrEmpty(dieTriggerParm))
        {
            animator.SetTrigger(dieTriggerHash);
        }
    }
}
