using UnityEngine;

public class QuestDetailUI : MonoBehaviour
{
    public static QuestDetailUI Instance;

    public TMPro.TMP_Text titleText;
    public TMPro.TMP_Text descText;

    void Awake()
    {
        Instance = this;
    }

    public void Show(Quest_Data_Table quest)
    {
        titleText.text = quest.QuestName;
        descText.text = quest.CompleteCondition;
    }
}

