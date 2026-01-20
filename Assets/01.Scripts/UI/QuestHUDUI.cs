using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class QuestHUDUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Image backgroundImage;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI progressText;
    [SerializeField] private TextMeshProUGUI distanceText;
    
    [Header("Colors")]
    [SerializeField] private Color normalColor = new Color(0f, 0f, 0f, 0.6f);
    [SerializeField] private Color completedColor = new Color(0.2f, 0.8f, 0.3f, 0.8f);

    private Transform player;
    private Transform currentTarget; 
    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
    }

    void Update()
    {
        int trackedId = QuestManager.Instance.GetTrackedQuestId();
        if (trackedId == -1)
        {
            gameObject.SetActive(false);
            currentTarget = null; //리셋
            return;
        }

        var quest = QuestManager.Instance.GetQuestData(trackedId);
        if (quest == null)
            return;

        gameObject.SetActive(true);

        // 제목
        titleText.text = quest.QuestName;

        // 진행도
        var state = QuestManager.Instance.GetQuestState(trackedId);
        bool isCompleted =
            state == QuestState.Completed ||
            state == QuestState.RewardClaimed;
        if (isCompleted)
        {
            backgroundImage.color = completedColor;
            progressText.text = "보상 수령 가능";
            distanceText.text = "";
            currentTarget = null;
            return;
        }

        // 진행 중 상태
        backgroundImage.color = normalColor;

        var progress = QuestManager.Instance.GetProgress(trackedId);
        if (progress != null)
        {
            progressText.text = $"진행도 ( {progress.Current} / {progress.Target} )";
        }

        // 타겟 갱신 시도
        var target = QuestManager.Instance.GetQuestTarget(trackedId);
        if (target != null)
        {
            currentTarget = target;
        }

        // 거리 표시
        if (currentTarget != null)
        {
            float dist = Vector3.Distance(player.position, currentTarget.position);
            distanceText.text = $"{Mathf.FloorToInt(dist)}m";
        }
        else
        {
            distanceText.text = "";
        }

        Debug.Log($"[HUD] trackedQuestId={trackedId}, TargetID={quest.TargetID}, Condition={quest.CompleteCondition}");
    }
}