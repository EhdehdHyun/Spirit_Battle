using System.Collections.Generic;
using System.Linq;
using UnityEngine;
public enum QuestState
{
    Active,           // 진행중 
    Completed,        // 완료됨(보상 미수령)
    RewardClaimed     // 보상 수령 완료
}
public enum CompleteCondition
{
    Auto,          // 자동 완료
    TalkToNPC,     // NPC 대화
    KillMonster,   // 몬스터 처치
    UseSkill,      // 스킬 사용 (튜토리얼 핵심)
    Investigate,   // 조사/상호작용
    CollectItem ,  //아이템 수집
    DestroyObject  //오브젝트 파괴
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
        
       
        AcceptQuest(30000); //Main 퀘스트 바로 시작
        CompleteQuest(30000); // 즉시 완료
        AcceptQuest(40000); //Tutorial 퀘스트 바로 시작
    }
    public int GetTrackedQuestId()
    {
        return trackedQuestId;
    }
    
    //카테고리별 퀘스트 가져오기 (UI용)
    public void AcceptQuest(int questId, int npcID = -1)
    {
        if (questStates.ContainsKey(questId))
            return;

        var quest = questTable[questId];

        // TalkToNPC 시작 조건 검사
        if (quest.StartCondition == "TalkToNPC" && npcID != -1)
        {
            if (quest.NPC != npcID)
            {
                Debug.Log($"[Quest] NPC {npcID}는 퀘스트 {questId}를 줄 수 없음");
                return;
            }
        }

        questStates.Add(questId, QuestState.Active);
        questProgress.Add(questId, new QuestProgress(quest.TargetCount));
        trackedQuestId = questId;
        
        if (quest.DeliverItemID > 0 && quest.TargetCount > 0)
        {
            var itemData = GameManager.Instance
                .Data
                .Data_TableLoader
                .ItemsDict[quest.DeliverItemID];

            InventoryManager.Instance.AddItem(itemData, quest.TargetCount);

            Debug.Log($"[Quest] 전달 아이템 지급: {itemData.ItemName} x{quest.TargetCount}");
        }

        Debug.Log($"[Quest] Accepted: {quest.QuestName}");
    }


    
    //퀘스트 완료
    public void CompleteQuest(int questId)
    {
        if (!questStates.ContainsKey(questId)) return;
        questStates[questId] = QuestState.Completed;
        Debug.Log($"[Quest] Completed: {questTable[questId].QuestName}");
        
        var quest = questTable[questId];

        // 다음 퀘스트 자동 시작
        if (quest.NextQuest > 0 && !questStates.ContainsKey(quest.NextQuest))
        {
            var nextQuest = questTable[quest.NextQuest];

            // Auto / TalkToNPC는 여기서 처리
            if (nextQuest.StartCondition == "Auto")
            {
                AcceptQuest(nextQuest.QuestID);
                SetTrackedQuest(nextQuest.QuestID);
            }
        }
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
    }
    
    // 퀘스트 완료 조건이 Talk To NPC인지 확인
    public bool TryCompleteTalkToNPCQuest(int npcID)
    {
        foreach (var kv in questStates)
        {
            int questId = kv.Key;

            if (kv.Value != QuestState.Active)
                continue;

            var quest = questTable[questId];

            // 완료 조건 검사
            if (quest.CompleteCondition != "TalkToNPC")
                continue;

            // 이 NPC가 대상 NPC인지
            if (quest.TargetID != npcID)
                continue;

            // 전달 아이템이 있다면 검사
            if (quest.DeliverItemID > 0)
            {
                if (!InventoryManager.Instance.HasItem(
                        quest.DeliverItemID,
                        quest.TargetCount))
                {
                    Debug.Log("아이템이 부족합니다.");
                    return false;
                }

                InventoryManager.Instance.RemoveItem(
                    quest.DeliverItemID,
                    quest.TargetCount
                );
            }

            // 퀘스트 완료
            CompleteQuest(questId);
            Debug.Log($"[Quest] 전달 완료: {questId}");
            return true;
        }

        return false;
    }
    public Transform GetQuestTarget(int questId)
    {
        if (!questTable.ContainsKey(questId))
            return null;

        var quest = questTable[questId];

        switch (quest.CompleteCondition)
        {
            case "KillMonster":
            {
                var player = GameObject.FindGameObjectWithTag("Player").transform;
                return QuestTargetRegistry.Instance
                    .GetClosestTarget(quest.TargetID, player.position);
            }

            case "Investigate":
            case "DestroyObject":
                return QuestTargetRegistry.Instance
                    .GetAnyTarget(quest.TargetID);
        }

        return null;
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
                if (!quest.RequireTurnIn)
                {
                    CompleteQuest(questId);
                }
                else
                {
                    Debug.Log($"[Quest] 수집 완료, NPC에게 보고 필요");
                }
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
    public bool CanTurnIn(int questId, int npcID)
    {
        if (!questStates.ContainsKey(questId)) return false;
        if (questStates[questId] != QuestState.Active) return false;

        var quest = questTable[questId];

        // 이 NPC가 보고 NPC인지 확인
        if (quest.NPC != npcID) return false;

        // 진행도 완료 여부 확인
        if (!questProgress.TryGetValue(questId, out var progress)) return false;
        return progress.IsComplete;
    }
    public void OnMonsterKilled(int monsterId)
    {
        ReportProgress(CompleteCondition.KillMonster, monsterId, 1);
    }
    private void OnEnable()
    {
        MonsterKillEvent.OnMonsterKilled += OnMonsterKilled;
    }

    private void OnDisable()
    {
        MonsterKillEvent.OnMonsterKilled -= OnMonsterKilled;
    }
    public void OnInvestigate(int investigateID)
    {
        ReportProgress(CompleteCondition.Investigate, investigateID, 1);
    }
    private bool ShouldShowProgress(CompleteCondition condition)
    {
        return condition == CompleteCondition.KillMonster
               || condition == CompleteCondition.CollectItem
               || condition == CompleteCondition.UseSkill
               || condition == CompleteCondition.Investigate
               || condition == CompleteCondition.DestroyObject;
    }
    private bool TryGetCondition(string raw, out CompleteCondition condition)
    {
        condition = CompleteCondition.Auto;

        if (string.IsNullOrEmpty(raw))
            return false;

        string normalized = raw.Trim().TrimEnd('.');

        return System.Enum.TryParse(normalized, out condition);
    }
    public Quest_Data_Table GetQuestData(int questId)
    {
        if (questTable == null)
        {
            Debug.LogError("QuestTable이 초기화되지 않았습니다.");
            return null;
        }

        if (!questTable.TryGetValue(questId, out var quest))
        {
            Debug.LogError($"Quest 데이터 없음: {questId}");
            return null;
        }

        return quest;
    }
    public QuestProgress GetProgress(int questId)
    {
        if (!questProgress.ContainsKey(questId))
            return null;

        return questProgress[questId];
    }
}