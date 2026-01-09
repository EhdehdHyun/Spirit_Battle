using UnityEngine;

public class QuestCategoryUI : MonoBehaviour
{
    public string questType;
    public Transform questListParent;
    public QuestItemUI questItemPrefab;

    void Start()
    {
        Debug.Log("QuestCategoryUI.Start 호출됨");
        Refresh();
    }

    public void Refresh()
    {
        Debug.Log("=== QuestCategoryUI.Refresh START ===");

        Debug.Log("QuestManager.Instance = " + QuestManager.Instance);
        Debug.Log("questListParent = " + questListParent);
        Debug.Log("questItemPrefab = " + questItemPrefab);
        Debug.Log("questType = " + questType);
        
        foreach (Transform child in questListParent)
        {
            Destroy(child.gameObject);
        }
        
        var quests = QuestManager.Instance.GetQuestsByType(questType);
        Debug.Log("quests = " + quests);
        
        foreach (var quest in quests)
        {
            var item = Instantiate(questItemPrefab, questListParent);
            item.SetData(quest);
        }

        Debug.Log("=== QuestCategoryUI.Refresh END ===");
    }
}