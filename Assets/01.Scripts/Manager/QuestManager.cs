using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance;

    private Dictionary<int, Quest_Data_Table> questTable;
    private HashSet<int> activeQuests = new();
    private HashSet<int> completedQuests = new();

    void Awake()
    {
        Instance = this;
    }
    void Start()
    {
        questTable = GameManager.Instance
            .Data
            .Quest_Data_Loader
            .ItemsDict;
        
        // 테스트용 메인 퀘스트 1번 자동 수락
        AcceptQuest(30000);
        CompleteQuest(30000);        // 즉시 완료
    }


    //퀘스트 수락
    public void AcceptQuest(int questId)
    {
        if (!activeQuests.Contains(questId))
            activeQuests.Add(questId);
    }

    //카테고리별 퀘스트 가져오기 (UI용)
    public IEnumerable<Quest_Data_Table> GetQuestsByType(string type)
    {
        return activeQuests
            .Select(id => questTable[id])
            .Where(q => q.QuestType == type);
    }

    //퀘스트 완료
    public void CompleteQuest(int questId)
    {
        activeQuests.Remove(questId);
        completedQuests.Add(questId);

        var quest = questTable[questId];
        if (quest.NextQuest > 0)
            AcceptQuest(quest.NextQuest);
    }
}