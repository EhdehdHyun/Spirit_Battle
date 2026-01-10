using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class QuestItemUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI questTitleText;

    private static QuestItemUI currentSelected;
    private Quest_Data_Table questData;

    public void SetData(Quest_Data_Table quest)
    {
        questData = quest;
        questTitleText.text = quest.QuestName;
    }
    public void OnClick()
    {
        if (currentSelected != null)
            currentSelected.SetSelected(false);

        currentSelected = this;
        SetSelected(true);

        QuestDetailUI.Instance.SetQuest(questData);
    }
    void SetSelected(bool selected)
    {
        // (나중에 텍스트 색, 아이콘등등 되면 추가
    }
}
