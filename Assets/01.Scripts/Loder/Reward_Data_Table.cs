using System;
using System.Collections.Generic;
using UnityEngine;
[Serializable]
public class Reward_Data_Table
{
    /// <summary>
    /// Reward Group ID
    /// </summary>
    public int RewardGroupID;

    /// <summary>
    /// Exp Reward
    /// </summary>
    public int Exp;

    /// <summary>
    /// Gold Reward
    /// </summary>
    public int Gold;

    /// <summary>
    /// Item ID (0이면 없음)
    /// </summary>
    public int ItemID;

    /// <summary>
    /// Item Count
    /// </summary>
    public int ItemCount;

    /// <summary>
    /// Notes (설명용)
    /// </summary>
    public string Notes;
}
public class Reward_Data_Loader
{
    public List<Reward_Data_Table> ItemsList { get; private set; }
    public Dictionary<int, Reward_Data_Table> ItemsDict { get; private set; }

    public Reward_Data_Loader(string path = "JSON/Reward_Data_Table")
    {
        TextAsset json = Resources.Load<TextAsset>(path);

        if (json == null)
        {
            Debug.LogError($"Reward_Data_Table JSON not found : {path}");
            return;
        }

        ItemsList = JsonUtility.FromJson<Wrapper>(json.text).Items;
        Debug.Log($"[RewardData] Loaded Count : {ItemsList.Count}");

        ItemsDict = new Dictionary<int, Reward_Data_Table>();
        foreach (var item in ItemsList)
        {
            ItemsDict.Add(item.RewardGroupID, item);
        }
    }

    [Serializable]
    private class Wrapper
    {
        public List<Reward_Data_Table> Items;
    }

    /// <summary>
    /// RewardGroupID로 보상 데이터 가져오기
    /// </summary>
    public Reward_Data_Table GetByGroupID(int groupId)
    {
        if (ItemsDict.ContainsKey(groupId))
            return ItemsDict[groupId];

        return null;
    }
}
