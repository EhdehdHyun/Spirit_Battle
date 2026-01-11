using TMPro;
using UnityEngine;

public class QuestDetailUI : MonoBehaviour
{
    public static QuestDetailUI Instance;

    [Header("Texts")]
    [SerializeField] private TextMeshProUGUI questTitle;
    [SerializeField] private TextMeshProUGUI questPurpose;
    [SerializeField] private TextMeshProUGUI questDescription;
    [SerializeField] private Transform rewardGrid;
    [SerializeField] private RewardItemUI rewardItemPrefab;
    [SerializeField] private Sprite expIconSprite;
    [SerializeField] private Sprite goldIconSprite;
    
    private Quest_Data_Table currentQuest;

    void Awake()
    {
        Instance = this;
    }

    public void SetQuest(Quest_Data_Table quest)
    {
        currentQuest = quest;
        questTitle.text = quest.QuestName;
        questPurpose.text = quest.CompleteCondition;
        questDescription.text = quest.Description;
        
        ShowRewardPreview(quest.RewardGroupID);
    }
    void ShowRewardPreview(int rewardGroupId)
    {
        foreach (Transform child in rewardGrid)
            Destroy(child.gameObject);

        var reward = GameManager.Instance
            .Data
            .Reward_Data_Loader
            .ItemsDict[rewardGroupId];

        if (reward.Exp > 0)
            CreateRewardItem(expIconSprite, reward.Exp);

        if (reward.Gold > 0)
            CreateRewardItem(goldIconSprite, reward.Gold);
    }
    public void OnClickClaimReward()
    {
        QuestManager.Instance.ClaimReward(currentQuest.QuestID);
        
        QuestUIController.Instance.RefreshAll();

        // 다음 퀘스트 자동 선택
        QuestUIController.Instance.SelectFirstQuest();
    }
    
    void CreateRewardItem(Sprite icon, int amount)
    {
        var ui = Instantiate(rewardItemPrefab, rewardGrid);
        ui.Set(icon, amount);
    }

}