using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class Quest_Data_Table
{
    /// <summary>
    /// Quest ID
    /// </summary>
    public int QuestID;

    /// <summary>
    /// Quest Type (Main / Tutorial / Side)
    /// </summary>
    public string QuestType;

    /// <summary>
    /// Quest Name
    /// </summary>
    public string QuestName;

    /// <summary>
    /// NPC ID
    /// </summary>
    public int NPC;

    /// <summary>
    /// Start Condition
    /// </summary>
    public string StartCondition;
    
    /// <summary>
    /// Quest Desciption
    ///</summary>
    public string Description;

    /// <summary>
    /// Complete Condition
    /// </summary>
    public string CompleteCondition;

    /// <summary>
    /// Target ID
    /// </summary>
    public int TargetID;

    /// <summary>
    /// Target Count
    /// </summary>
    public int TargetCount;

    /// <summary>
    /// Reward Group ID
    /// </summary>
    public int RewardGroupID;

    /// <summary>
    /// Next Quest ID
    /// </summary>
    public int NextQuest;
    
    /// <summary>
    /// Quest Purpose
    /// </summary>
    public string ObjectiveText; 
    
    /// <summary>
    /// NPC에게 보고해야 완료
    /// </summary>
    public bool RequireTurnIn;
    
    /// <summary>
    /// 전달퀘스트 아이템 아이디
    /// </summary>
    public int DeliverItemID; 
    
    /// <summary>
    /// Target ID 우선권
    /// </summary>
    public int HUDTargetID;
    
    /// <summary>
    /// Invest퀘스트시에 Item ID말고 CollectItemID를 추적
    /// </summary>
    public int CollectItemID;
}

public class Quest_Data_Loader
{
    public List<Quest_Data_Table> ItemsList { get; private set; }
    public Dictionary<int, Quest_Data_Table> ItemsDict { get; private set; }

    public Quest_Data_Loader(string path = "JSON/Quest_Data_Table")
    {
        TextAsset json = Resources.Load<TextAsset>(path);

        if (json == null)
        {
            return;
        }

        ItemsList = JsonUtility.FromJson<Wrapper>(json.text).Items;

        ItemsDict = new Dictionary<int, Quest_Data_Table>();
        foreach (var item in ItemsList)
        {
            ItemsDict.Add(item.QuestID, item);
        }
    }

    [Serializable]
    private class Wrapper
    {
        public List<Quest_Data_Table> Items;
    }

    /// <summary>
    /// QuestID로 데이터 가져오기
    /// </summary>
    public Quest_Data_Table GetByQuestID(int questId)
    {
        if (ItemsDict.ContainsKey(questId))
            return ItemsDict[questId];

        return null;
    }
}