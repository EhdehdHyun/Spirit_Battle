using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class QuestItemUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI questTitleText;

    private Quest_Data_Table questData;

    public void SetData(Quest_Data_Table quest)
    {
        questData = quest;
        questTitleText.text = quest.QuestName;
    }
    public void OnClick()
    {
        QuestDetailUI.Instance.ShowQuest(questData);
    }
}
