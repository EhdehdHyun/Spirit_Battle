using System.Collections.Generic;
using System.Linq;
using UnityEngine;
public enum QuestState
{
    Active,
    Completed,        // 완료됨(보상 미수령)
    RewardClaimed     // 보상 수령 완료
}

public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance;

    private Dictionary<int, QuestState> questStates = new();
    private Dictionary<int, Quest_Data_Table> questTable;

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
        CompleteQuest(30000);   // 즉시 완료
    }
    
    //카테고리별 퀘스트 가져오기 (UI용)
    public void AcceptQuest(int questId)
    {
        if (!questStates.ContainsKey(questId))
            questStates.Add(questId, QuestState.Active);
    }

    //퀘스트 완료
    public void CompleteQuest(int questId)
    {
        if (!questStates.ContainsKey(questId)) return;
        questStates[questId] = QuestState.Completed;
    }
    public void ClaimReward(int questId)
    {
        if (!questStates.ContainsKey(questId)) return;
        if (questStates[questId] != QuestState.Completed) return;

        var quest = questTable[questId];

        var reward = GameManager.Instance
            .Data
            .Reward_Data_Loader
            .ItemsDict[quest.RewardGroupID];

        // 임시 보상 처리 (로그)
        Debug.Log($"보상 지급: EXP {reward.Exp}, GOLD {reward.Gold}");

        questStates[questId] = QuestState.RewardClaimed;

        if (quest.NextQuest > 0)
            AcceptQuest(quest.NextQuest);
    }
    
    public QuestState GetQuestState(int questId)
    {
        if (!questStates.ContainsKey(questId))
            return QuestState.Active;

        return questStates[questId];
    }

    public IEnumerable<Quest_Data_Table> GetQuestsByType(string type)
    {
        return questStates
            .Where(kv =>
                kv.Value == QuestState.Active ||
                kv.Value == QuestState.Completed)
            .Select(kv => questTable[kv.Key])
            .Where(q => q.QuestType == type);
    }

}