using UnityEngine;

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
        Refresh();
    }
    public void Refresh()
    {
        if (questListParent == null)
        {
            Debug.LogError("questListParent is NULL");
            return;
        }

        if (questItemPrefab == null)
        {
            Debug.LogError("questItemPrefab is NULL");
            return;
        }

        if (QuestManager.Instance == null)
        {
            Debug.LogError("QuestManager.Instance is NULL");
            return;
        }
        
        var quests = QuestManager.Instance.GetQuestsByType(questType);
        if (quests == null)
        {
            Debug.LogWarning("quests is NULL");
            return;
        }

        foreach (Transform child in questListParent)
            Destroy(child.gameObject);

        foreach (var quest in quests)
        {
            var item = Instantiate(questItemPrefab, questListParent);
            item.SetData(quest);
        }
    }
    public void SelectFirstQuest()
    {
        if (questListParent.childCount == 0) return;

        var firstItem = questListParent
            .GetChild(0)
            .GetComponent<QuestItemUI>();

        firstItem.OnClick();
    }
}