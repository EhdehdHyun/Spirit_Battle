using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ItemSaveData
{
    public int slotIndex;
    public int itemKey;
    public int amount;
}

[System.Serializable]
public class SaveData
{
    public int level;
    public float currentExp;
    public float currentHp;
    public Vector3 playerPosition;

    public List<ItemSaveData> inventoryItems = new List<ItemSaveData>();

    public SaveData()
    {
        level = 1;
        currentExp = 0;
        currentHp = 100;
        playerPosition = Vector3.zero;
    }
}