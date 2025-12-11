using UnityEngine;

public class MonsterAnimation : MonoBehaviour
{
    [Header("애니메이터")]
    [SerializeField] private Animator animator;

    [Header("파라미터 이름 설정")] //애니메이터에서 각각
    [SerializeField] private string moveSpeedParm = "MoveSpeed"; //float
    [SerializeField] private string isRunningParm = "IsRunning"; //bool
    [SerializeField] private string dieTriggerParm = "Die"; //triger

    private int moveSpeedHash;
    private int isRunningHash;
    private int dieTriggerHash;

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
