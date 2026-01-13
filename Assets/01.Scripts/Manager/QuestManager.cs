using System.Collections.Generic;
using System.Linq;
using UnityEngine;
public enum QuestState
{
    Active,
    Completed,        // 완료됨(보상 미수령)
    RewardClaimed     // 보상 수령 완료
}
public enum CompleteCondition
{
    Auto,          // 자동 완료
    TalkToNPC,     // NPC 대화
    KillMonster,   // 몬스터 처치
    UseSkill,      // 스킬 사용 (튜토리얼 핵심)
    Investigate,    // 조사/상호작용
    CollectItem  //아이템 수집
}

public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance;

    private Dictionary<int, QuestState> questStates = new();
    private Dictionary<int, Quest_Data_Table> questTable;
    private Dictionary<int, QuestProgress> questProgress = new();
    
    [SerializeField] private QuestTrackerUI trackerUI;  
    
    private int trackedQuestId = -1;
    
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

        var quest = questTable[questId];

        questProgress.Add(
            questId,
            new QuestProgress(quest.TargetCount)
        );

        trackedQuestId = questId;

        if (!TryGetCondition(quest.CompleteCondition, out var condition))
        {
            Debug.LogError($"Invalid CompleteCondition: {quest.CompleteCondition}");
            return;
        }

        if (trackerUI != null)
        {
            if (ShouldShowProgress(condition))
            {
                var progress = questProgress[questId];
                trackerUI.gameObject.SetActive(true);
                trackerUI.SetProgress(progress.Current, progress.Target); // ⭐ 핵심
            }
            else
            {
                trackerUI.gameObject.SetActive(false);
            }
        }

        if (condition == CompleteCondition.Auto)
        {
            CompleteQuest(questId);
        }

        Debug.Log($"[Quest] Accepted: {quest.QuestName}");
    }



    //퀘스트 완료
    public void CompleteQuest(int questId)
    {
        if (!questStates.ContainsKey(questId)) return;
        questStates[questId] = QuestState.Completed;
        
        questStates[questId] = QuestState.Completed;
        Debug.Log($"[Quest] Completed: {questTable[questId].QuestName}");
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
    public bool HasQuest(int questId)
    {
        return questStates.ContainsKey(questId);
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
    public void ReportProgress(
        CompleteCondition condition,
        int targetId,
        int amount = 1
    )
    {
        // 진행도 HUD 대상이 아니면 아예 무시
        if (!ShouldShowProgress(condition))
            return;
        
        if (trackedQuestId == -1)
            return;

        foreach (var questId in questStates.Keys.ToList())
        {
            if (questStates[questId] != QuestState.Active)
                continue;

            var quest = questTable[questId];

            if (quest.CompleteCondition != condition.ToString())
                continue;

            if (quest.TargetID != targetId)
                continue;

            var progress = questProgress[questId];
            progress.Current += amount;
            
            Debug.Log(
                $"[QuestProgress] questId={questId}, " +
                $"questName={quest.QuestName}, " +
                $"condition={quest.CompleteCondition}, " +
                $"current={progress.Current}/{progress.Target}, " +
                $"trackedQuestId={trackedQuestId}"
            );
            
            var trackedQuest = questTable[trackedQuestId];
            if (!TryGetCondition(trackedQuest.CompleteCondition, out var trackedCondition))
                continue;

            if (questId == trackedQuestId && condition == trackedCondition)
            {
                trackerUI.SetProgress(progress.Current, progress.Target);
            }

            if (progress.IsComplete)
            {
                CompleteQuest(questId);
            }
        }
    }
    public void SetTrackedQuest(int questId)
    {
        if (!questStates.ContainsKey(questId)) return;
        trackedQuestId = questId;

        var quest = questTable[questId];
        if (!TryGetCondition(quest.CompleteCondition, out var cond))
            return;

        if (trackerUI == null) return;

        if (!ShouldShowProgress(cond))
        {
            trackerUI.gameObject.SetActive(false);
            return;
        }

        trackerUI.gameObject.SetActive(true);

        var p = questProgress[questId];
        trackerUI.SetProgress(p.Current, p.Target);

        Debug.Log($"[Quest] 퀘스트 Tracke변경-> {questId} ({quest.QuestName})");
    }
    public void OnMonsterKilled(int monsterId)
    {
        ReportProgress(CompleteCondition.KillMonster, monsterId, 1);
    }
    private bool ShouldShowProgress(CompleteCondition condition)
    {
        return condition == CompleteCondition.KillMonster
               || condition == CompleteCondition.CollectItem
               || condition == CompleteCondition.UseSkill;
    }
    private bool TryGetCondition(string raw, out CompleteCondition condition)
    {
        condition = CompleteCondition.Auto;

        if (string.IsNullOrEmpty(raw))
            return false;

        string normalized = raw.Trim().TrimEnd('.');

        return System.Enum.TryParse(normalized, out condition);
    }

}