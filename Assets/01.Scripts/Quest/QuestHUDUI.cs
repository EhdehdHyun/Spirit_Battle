using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class QuestHUDUI : MonoBehaviour
{
    public static QuestHUDUI Instance;
    
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
    
    private void Awake()
    {
        Instance = this;
    }
    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
    }
    private void ClearHUD()
    {
        titleText.text = "";
        progressText.text = "";
        distanceText.text = "";
        currentTarget = null;
        backgroundImage.color = normalColor;
    }

    void Update()
    {
        int trackedId = QuestManager.Instance.GetTrackedQuestId();
        if (trackedId == -1)
        {
            ClearHUD(); 
            return;
        }

        var quest = QuestManager.Instance.GetQuestData(trackedId);
        if (quest == null)
            return;

        // 제목
        titleText.text = quest.QuestName;

        // 진행도
        var state = QuestManager.Instance.GetQuestState(trackedId);
        
        if (state == QuestState.RewardClaimed)
        {
            ClearHUD(); 
            return;
        }

        if (state == QuestState.Completed)
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
    }
    public void ClearCurrentTarget()
    {
        currentTarget = null;
        distanceText.text = "";
    }
    private void OnEnable()
    {
        QuestManager.Instance.OnQuestStateChanged += OnQuestStateChanged;
    }
    private void OnDisable()
    {
        QuestManager.Instance.OnQuestStateChanged -= OnQuestStateChanged;
    }
    private void OnQuestStateChanged(int questId, QuestState state)
    {
        // 수령 완료면 HUD에서 제거
        if (state == QuestState.RewardClaimed)
        {
  
        }
    }
}