using TMPro;
using UnityEngine;

public class QuestDetailUI : MonoBehaviour
{
    public static QuestDetailUI Instance;

    [Header("Texts")]
    [SerializeField] private TextMeshProUGUI questTitle;
    [SerializeField] private TextMeshProUGUI questPurpose;
    [SerializeField] private TextMeshProUGUI questDescription;

    void Awake()
    {
        Instance = this;
    }

    public void ShowQuest(Quest_Data_Table quest)
    {
        questTitle.text = quest.QuestName;
        questPurpose.text = quest.CompleteCondition;
        questDescription.text = quest.Description;
    }
}