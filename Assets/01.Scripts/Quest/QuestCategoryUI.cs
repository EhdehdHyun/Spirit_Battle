using UnityEngine;
using UnityEngine.UI;

public class QuestCategoryUI : MonoBehaviour
{
    public string questType;
    public Transform questListParent;
    public QuestItemUI questItemPrefab;
    
    void OnEnable()
    {
        if (QuestManager.Instance == null)
        {
            Debug.LogWarning("QuestManager not ready yet");
            return;
        }
        QuestDetailUI.OnQuestClaimed += OnQuestClaimed;
        Refresh();
    }
    private void OnDisable()
    {
        QuestDetailUI.OnQuestClaimed -= OnQuestClaimed;
    }

    private void OnQuestClaimed(int questId)
    {
        Refresh();

        LayoutRebuilder.ForceRebuildLayoutImmediate(
            questListParent as RectTransform
        );

        SelectFirstQuest();
    }
    public void Refresh()
    {
        foreach (Transform child in questListParent)
            Destroy(child.gameObject);

        var quests = QuestManager.Instance.GetQuestsByType(questType);

        foreach (var quest in quests)
        {
            var item = Instantiate(questItemPrefab, questListParent);
            item.SetData(quest);
        }
    }

    public void SelectFirstQuest()
    {
        if (questListParent.childCount == 0)
        {
            QuestUIController.Instance.ClearDetail();
            return;
        }

        questListParent
            .GetChild(0)
            .GetComponent<QuestItemUI>()
            .OnClick();
    }
}