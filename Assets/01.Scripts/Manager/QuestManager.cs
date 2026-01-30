using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System;
public enum QuestState
{
    Active,
    Completed,
    RewardClaimed
}

public enum CompleteCondition
{
    Auto,
    TalkToNPC,
    KillMonster,
    UseSkill,
    Investigate,
    CollectItem,
    DestroyObject
}

public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance;

    private Dictionary<int, QuestState> questStates = new();
    private Dictionary<int, Quest_Data_Table> questTable;
    private Dictionary<int, QuestProgress> questProgress = new();

    [SerializeField] private QuestTrackerUI trackerUI;

    private int trackedQuestId = -1;
    public event Action<int, QuestState> OnQuestStateChanged;
    public Transform PlayerTransform { get; private set; }

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        if (GameManager.Instance != null && GameManager.Instance.Data != null)
        {
            questTable = GameManager.Instance.Data.Quest_Data_Loader.ItemsDict;
        }

        PlayerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;
    }

    public void SaveToData(SaveData data)
    {
        data.questDataList.Clear();

        foreach (var kv in questStates)
        {
            int qId = kv.Key;
            QuestState state = kv.Value;
            int progress = 0;

            if (questProgress.ContainsKey(qId))
            {
                progress = questProgress[qId].Current;
            }

            QuestSaveData qData = new QuestSaveData();
            qData.questId = qId;
            qData.state = state;
            qData.currentProgress = progress;

            data.questDataList.Add(qData);
        }

        data.trackedQuestId = trackedQuestId;
    }

    public void LoadFromData(SaveData data)
    {
        questStates.Clear();
        questProgress.Clear();
        trackedQuestId = -1;

        if (data.questDataList == null || data.questDataList.Count == 0)
        {
            InitNewGameQuests();
            return;
        }

        foreach (var qData in data.questDataList)
        {
            questStates.Add(qData.questId, qData.state);

            if (questTable.TryGetValue(qData.questId, out var tableData))
            {
                QuestProgress prog = new QuestProgress(tableData.TargetCount);
                prog.Current = qData.currentProgress;
                questProgress.Add(qData.questId, prog);
            }
        }

        if (data.trackedQuestId != -1 && questStates.ContainsKey(data.trackedQuestId))
        {
            if (questStates[data.trackedQuestId] == QuestState.Active)
            {
                SetTrackedQuest(data.trackedQuestId);
            }
        }
    }

    private void InitNewGameQuests()
    {
        AcceptQuest(30000);
        CompleteQuest(30000);
        AcceptQuest(40000);
    }

    public int GetTrackedQuestId()
    {
        return trackedQuestId;
    }

    public void AcceptQuest(int questId, int npcID = -1)
    {
        if (questStates.ContainsKey(questId))
            return;

        if (!questTable.ContainsKey(questId)) return;

        var quest = questTable[questId];

        if (quest.StartCondition == "TalkToNPC" && npcID != -1)
        {
            if (quest.NPC != npcID)
            {
                return;
            }
        }

        questStates.Add(questId, QuestState.Active);
        questProgress.Add(questId, new QuestProgress(quest.TargetCount));

        SetTrackedQuest(questId);

        if (quest.DeliverItemID > 0 && quest.TargetCount > 0)
        {
            var itemData = GameManager.Instance
                .Data
                .Data_TableLoader
                .ItemsDict[quest.DeliverItemID];

            InventoryManager.Instance.AddItem(itemData, quest.TargetCount);
        }
    }

    public void CompleteQuest(int questId)
    {
        if (!questStates.ContainsKey(questId)) return;
        questStates[questId] = QuestState.Completed;

        var quest = questTable[questId];

        if (trackedQuestId == questId)
        {
            if (trackerUI != null) trackerUI.gameObject.SetActive(false);
            trackedQuestId = -1;
        }

        if (quest.NextQuest > 0 && !questStates.ContainsKey(quest.NextQuest))
        {
            var nextQuest = questTable[quest.NextQuest];

            if (nextQuest.StartCondition == "Auto")
            {
                AcceptQuest(nextQuest.QuestID);
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

        questStates[questId] = QuestState.RewardClaimed;

        if (trackedQuestId == questId)
            trackedQuestId = -1;

        OnQuestStateChanged?.Invoke(questId, QuestState.RewardClaimed);
    }

    public bool TryCompleteTalkToNPCQuest(int npcID)
    {
        foreach (var kv in questStates)
        {
            int questId = kv.Key;

            if (kv.Value != QuestState.Active)
                continue;

            var quest = questTable[questId];

            if (quest.CompleteCondition != "TalkToNPC")
                continue;

            if (quest.TargetID != npcID)
                continue;

            if (quest.DeliverItemID > 0)
            {
                if (!InventoryManager.Instance.HasItem(
                        quest.DeliverItemID,
                        quest.TargetCount))
                {
                    return false;
                }

                InventoryManager.Instance.RemoveItem(
                    quest.DeliverItemID,
                    quest.TargetCount
                );
            }

            CompleteQuest(questId);
            return true;
        }

        return false;
    }

    public Transform GetQuestTarget(int questId)
    {
        if (!questTable.ContainsKey(questId))
            return null;

        var quest = questTable[questId];

        if (quest.HUDTargetID > 0)
        {
            return QuestTargetRegistry.Instance
                ?.GetAnyTarget(quest.HUDTargetID);
        }

        switch (quest.CompleteCondition)
        {
            case "KillMonster":
                {
                    if (PlayerTransform == null)
                        return null;

                    return QuestTargetRegistry.Instance
                        .GetClosestTarget(quest.TargetID, PlayerTransform.position);
                }

            case "Investigate":
            case "DestroyObject":
            case "CollectItem":
            case "TalkToNPC":
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

            switch (condition)
            {
                case CompleteCondition.CollectItem:
                    if (quest.CollectItemID != targetId)
                        continue;
                    break;

                default:
                    if (quest.TargetID != targetId)
                        continue;
                    break;
            }

            var progress = questProgress[questId];
            progress.Current += amount;

            if (questId == trackedQuestId)
            {
                if (trackerUI != null)
                    trackerUI.SetProgress(progress.Current, progress.Target);
            }

            if (progress.IsComplete)
            {
                if (!quest.RequireTurnIn)
                {
                    CompleteQuest(questId);
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
    }

    public bool CanTurnIn(int questId, int npcID)
    {
        if (!questStates.ContainsKey(questId)) return false;
        if (questStates[questId] != QuestState.Active) return false;

        var quest = questTable[questId];

        if (quest.NPC != npcID) return false;

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
               || condition == CompleteCondition.DestroyObject
               || condition == CompleteCondition.TalkToNPC;
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
            return null;
        }

        if (!questTable.TryGetValue(questId, out var quest))
        {
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

    public bool IsQuestActive(int questId)
    {
        return questStates.TryGetValue(questId, out var state)
               && state == QuestState.Active;
    }

    public bool IsQuestCompleted(int questId)
    {
        return questStates.TryGetValue(questId, out var state)
               && (state == QuestState.Completed || state == QuestState.RewardClaimed);
    }
}