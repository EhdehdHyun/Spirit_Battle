using UnityEngine;
using TMPro;

public class QuestHUDUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI progressText;
    [SerializeField] private TextMeshProUGUI distanceText;

    private Transform player;

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
            return;
        }

        var quest = QuestManager.Instance.GetQuestData(trackedId);
        if (quest == null)
            return;

        gameObject.SetActive(true);

        // 제목
        titleText.text = quest.QuestName;

        // 진행도 (target 없어도 항상 갱신)
        var progress = QuestManager.Instance.GetProgress(trackedId);
        if (progress != null)
        {
            progressText.text = $"진행도 ( {progress.Current} / {progress.Target} )";
        }

        // 거리 (target 있을 때만)
        var target = QuestManager.Instance.GetQuestTarget(trackedId);
        if (target != null)
        {
            float dist = Vector3.Distance(player.position, target.position);
            distanceText.text = $"{Mathf.FloorToInt(dist)}m";
        }
        else
        {
            distanceText.text = ""; // 또는 "완료"
        }
    }

}