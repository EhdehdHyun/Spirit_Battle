using TMPro;
using UnityEngine;

public class QuestItemUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI questTitleText;

    private Quest_Data_Table questData;

    public void SetData(Quest_Data_Table quest)
    {
        questData = quest;
        questTitleText.text = quest.QuestName;
    }

    // 클릭 시 상세창에 알려주기 (다음 단계용)
    public Quest_Data_Table GetQuestData()
    {
        return questData;
    }
}