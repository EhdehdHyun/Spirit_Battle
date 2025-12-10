using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ItemDebugData
{
    public string itemName;
    public int key;
    public int quantity;
}

public class TempInventory : MonoBehaviour
{
    public static TempInventory Instance { get; private set; }

    private List<ItemInstance> items = new List<ItemInstance>();

    [Header("TempInventory")]
    public List<ItemDebugData> debugItems = new List<ItemDebugData>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void AddItem(Data_table data, int quantity)
    {
        if (data == null)
        {
            Debug.LogError("[TempInventory] AddItem 호출 시 data 가 null 입니다.");
            return;
        }

        int remaining = quantity;

        if (data.MaxStack > 1)
        {
            foreach (var inst in items)
            {
                if (inst.data.key != data.key) continue;
                if (inst.quantity >= inst.data.MaxStack) continue;

                int canAdd = inst.data.MaxStack - inst.quantity;
                int add = Mathf.Min(canAdd, remaining);

                inst.quantity += add;
                remaining -= add;

                if (remaining <= 0)
                    break;
            }
        }

        while (remaining > 0)
        {
            int add = data.MaxStack > 0 ? Mathf.Min(data.MaxStack, remaining) : remaining;
            items.Add(new ItemInstance(data, add));
            remaining -= add;
        }

        RefreshDebugList();

        Debug.Log($"[TempInventory] {data.ItemName} x{quantity} 추가됨");
    }

    private void RefreshDebugList()
    {
        debugItems.Clear();

        foreach (var inst in items)
        {
            if (inst == null || inst.data == null) continue;

            debugItems.Add(new ItemDebugData
            {
                itemName = inst.data.ItemName,
                key = inst.data.key,
                quantity = inst.quantity
            });
        }
    }

    [ContextMenu("Print All Items")]
    public void PrintAllItems()
    {
        if (items.Count == 0)
        {
            Debug.Log("[TempInventory] 인벤토리 비어 있음");
            return;
        }

        Debug.Log("===== TempInventory 내용 =====");
        foreach (var inst in items)
        {
            if (inst == null || inst.data == null) continue;
            Debug.Log($"- {inst.data.ItemName} x{inst.quantity} (key: {inst.data.key})");
        }
        Debug.Log("================================");
    }

    // 필요하면 인스펙터에서 수동으로 새로고침할 수 있게
    [ContextMenu("Refresh Debug List")]
    public void RefreshDebugList_ContextMenu()
    {
        RefreshDebugList();
    }
}
