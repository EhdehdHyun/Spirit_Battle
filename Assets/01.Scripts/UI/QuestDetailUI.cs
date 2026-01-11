using TMPro;
using UnityEngine;

public class QuestDetailUI : MonoBehaviour
{

    [Header("Texts")]
    [SerializeField] private TextMeshProUGUI questTitle;
    [SerializeField] private TextMeshProUGUI questPurpose;
    [SerializeField] private TextMeshProUGUI questDescription;
    [SerializeField] private Transform rewardGrid;
    [SerializeField] private RewardItemUI rewardItemPrefab;
    [SerializeField] private Sprite expIconSprite;
    [SerializeField] private Sprite goldIconSprite;
    
    private Quest_Data_Table currentQuest;
    
    public void SetQuest(Quest_Data_Table quest)
    {
        Debug.Log($"[QuestDetailUI] SetQuest({quest.QuestName}) on {gameObject.name}, id={GetInstanceID()}");
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
        if (currentQuest == null)
            return;

        QuestManager.Instance.ClaimReward(currentQuest.QuestID);
        
        Clear();

        // 왼쪽 리스트 갱신
        QuestUIController.Instance.RefreshAll();
    }
    
    void CreateRewardItem(Sprite icon, int amount)
    {
        var ui = Instantiate(rewardItemPrefab, rewardGrid);
        ui.Set(icon, amount);
    }
    public void Clear()
    {
        currentQuest = null;

        questTitle.text = "";
        questPurpose.text = "";
        questDescription.text = "";

        foreach (Transform child in rewardGrid)
            Destroy(child.gameObject);
    }
}