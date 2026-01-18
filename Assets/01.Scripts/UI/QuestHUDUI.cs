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
        var target = QuestManager.Instance.GetQuestTarget(trackedId);

        if (quest == null || target == null)
            return;

        gameObject.SetActive(true);

        titleText.text = quest.QuestName;

        var progress = QuestManager.Instance.GetProgress(trackedId);
        progressText.text = $"진행도 ( {progress.Current} / {progress.Target} )";

        float dist = Vector3.Distance(player.position, target.position);
        distanceText.text = $"{Mathf.FloorToInt(dist)}m";
    }
}