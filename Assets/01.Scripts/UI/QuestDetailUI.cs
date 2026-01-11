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
    [SerializeField] private TMP_Text rewardAmountText;
    
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

        var rewardTable = GameManager.Instance
            .Data
            .Reward_Data_Loader
            .ItemsDict;

        if (!rewardTable.ContainsKey(rewardGroupId))
            return;

        var reward = rewardTable[rewardGroupId];

        rewardAmountText.text =
            $"EXP {reward.Exp} / GOLD {reward.Gold}";
    }
    public void OnClickClaimReward()
    {
        QuestManager.Instance.ClaimReward(currentQuest.QuestID);
    }
    public void Clear()
    {
        questTitle.text = "";
        questPurpose.text = "";
        questDescription.text = "";
        rewardAmountText.text = "";
    }

}