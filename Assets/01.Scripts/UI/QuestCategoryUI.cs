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
        if (questListParent.childCount == 0) return;
        questListParent.GetChild(0).GetComponent<QuestItemUI>().OnClick();
    }
}