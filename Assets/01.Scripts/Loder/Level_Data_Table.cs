using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class level_Data_Table
{
    /// <summary>
    /// Level
    /// </summary>
    public int level;

    /// <summary>
    /// Max HP
    /// </summary>
    public int MaxHP;

    /// <summary>
    /// Attack
    /// </summary>
    public int Attack;

    /// <summary>
    /// Stamina
    /// </summary>
    public int Stamina;

    /// <summary>
    /// Required Exp (다음 레벨까지)
    /// </summary>
    public int RequiredExp;
}

public class Level_Data_Loader
{
    public List<level_Data_Table> ItemsList { get; private set; }
    public Dictionary<int, level_Data_Table> ItemsDict { get; private set; }

    public Level_Data_Loader(string path = "JSON/level_Data_Table")
    {
        TextAsset json = Resources.Load<TextAsset>(path);

        if (json == null)
        {
            Debug.LogError($"Level_Data_Table JSON not found : {path}");
            return;
        }

        ItemsList = JsonUtility.FromJson<Wrapper>(json.text).Items;
        Debug.Log($"[LevelData] Loaded Count : {ItemsList.Count}");
        ItemsDict = new Dictionary<int, level_Data_Table>();

        foreach (var item in ItemsList)
        {
            ItemsDict.Add(item.level, item);
        }
    }

    [Serializable]
    private class Wrapper
    {
        public List<level_Data_Table> Items;
    }

    /// <summary>
    /// 레벨로 데이터 가져오기
    /// </summary>
    public level_Data_Table GetByLevel(int level)
    {
        if (ItemsDict.ContainsKey(level))
            return ItemsDict[level];

        return null;
    }
}