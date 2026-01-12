using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
    
    [Header("Reward Button")]
    [SerializeField] private Button claimButton;
    [SerializeField] private TextMeshProUGUI claimButtonText;
    [SerializeField] private Color claimedColor = Color.gray;
    
    private Quest_Data_Table currentQuest;
    
    public void SetQuest(Quest_Data_Table quest)
    {
        Debug.Log($"[QuestDetailUI] SetQuest({quest.QuestName}) on {gameObject.name}, id={GetInstanceID()}");
        currentQuest = quest;
        questTitle.text = quest.QuestName;
        questPurpose.text = quest.CompleteCondition;
        questDescription.text = quest.Description;

        ShowRewardPreview(quest.RewardGroupID);
        UpdateClaimButton();
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
        UpdateClaimButton();
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
    private void SetClaimedUI()
    {
        // 버튼 비활성화
        claimButton.interactable = false;

        // 텍스트 변경
        claimButtonText.text = "수령 완료";

        // 버튼 색 변경
        var colors = claimButton.colors;
        colors.normalColor = claimedColor;
        colors.disabledColor = claimedColor;
        claimButton.colors = colors;
    }
    private void ResetClaimButton()
    {
        claimButton.interactable = true;
        claimButtonText.text = "수령하기";

        var colors = claimButton.colors;
        colors.normalColor = Color.white;   // 원래 색
        colors.disabledColor = Color.gray;
        claimButton.colors = colors;
    }
    void UpdateClaimButton()
    {
        if (currentQuest == null)
            return;

        var state = QuestManager.Instance.GetQuestState(currentQuest.QuestID);

        switch (state)
        {
            case QuestState.Completed:
                claimButton.interactable = true;
                claimButtonText.text = "수령하기";
                claimButton.image.color = Color.white;
                break;

            case QuestState.RewardClaimed:
                claimButton.interactable = false;
                claimButtonText.text = "수령 완료";
                claimButton.image.color = Color.gray;
                break;

            default: // Active
                claimButton.interactable = false;
                claimButtonText.text = "진행 중";
                claimButton.image.color = Color.gray;
                break;
        }
    }

}