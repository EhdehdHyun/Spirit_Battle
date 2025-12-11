using System;
using System.Collections.Generic;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    [Header("Grid Size (Rows x Columns)")]
    public int rows = 5;
    public int columns = 5;

    [Tooltip("rows * columns 만큼 자동 생성됨")]
    public List<InventorySlot> slots = new List<InventorySlot>();

    public int SlotCount => rows * columns;

    // UI가 구독하는 이벤트
    public event Action OnInventoryChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        InitSlots();
        Debug.Log("[InventoryManager] Awake 호출됨, 슬롯 개수: " + SlotCount);
    }


    private void InitSlots()
    {
        slots = new List<InventorySlot>(SlotCount);
        for (int i = 0; i < SlotCount; i++)
        {
            slots.Add(new InventorySlot());
        }
    }
    public bool AddItem(ItemInstance newItem)
    {
        if (newItem == null || newItem.data == null || newItem.quantity <= 0)
        {
            Debug.LogWarning("[InventoryManager] AddItem 인자가 이상함");
            return false;
        }

        Debug.Log($"[InventoryManager] AddItem 호출: {newItem.data.ItemName} x{newItem.quantity}");

        int remaining = newItem.quantity;
        var data = newItem.data;
        int maxStack = data.MaxStack;

        // 1) 기존 스택 채우기
        for (int i = 0; i < slots.Count && remaining > 0; i++)
        {
            var slot = slots[i];
            if (!slot.CanStack(newItem))
                continue;

            int space = slot.GetStackSpace();
            int add = Mathf.Min(space, remaining);

            slot.item.quantity += add;
            remaining -= add;
        }

        // 2) 빈 슬롯에 새 스택 만들기
        for (int i = 0; i < slots.Count && remaining > 0; i++)
        {
            var slot = slots[i];
            if (!slot.IsEmpty)
                continue;

            int add = maxStack > 0 ? Mathf.Min(maxStack, remaining) : remaining;

            // 새 인스턴스를 만들어 슬롯에 보관
            slot.Set(new ItemInstance(data, add));
            remaining -= add;
        }

        if (remaining > 0)
        {
            Debug.LogWarning($"[InventoryManager] 인벤토리가 꽉 차서 {remaining} 개는 못 넣음");
        }

        NotifyChanged();
        return remaining == 0;
    }

    public bool AddItem(Data_table data, int amount)
    {
        if (data == null || amount <= 0)
            return false;

        var inst = new ItemInstance(data, amount);
        return AddItem(inst);
    }

    public void RemoveAt(int slotIndex, int amount)
    {
        if (slotIndex < 0 || slotIndex >= slots.Count) return;

        var slot = slots[slotIndex];
        if (slot.IsEmpty) return;

        slot.item.quantity -= amount;
        if (slot.item.quantity <= 0)
            slot.Clear();

        NotifyChanged();
    }

    public InventorySlot GetSlot(int index)
    {
        if (index < 0 || index >= slots.Count) return null;
        return slots[index];
    }

    public void ClearAll()
    {
        foreach (var slot in slots)
            slot.Clear();

        NotifyChanged();
    }

    private void NotifyChanged()
    {
        Debug.Log("[InventoryManager] OnInventoryChanged 호출");
        OnInventoryChanged?.Invoke();
    }


    [ContextMenu("Debug Print Contents")]
    private void DebugPrintContents()
    {
        Debug.Log("===== Inventory =====");
        for (int i = 0; i < slots.Count; i++)
        {
            var s = slots[i];
            if (s.IsEmpty)
                Debug.Log($"{i}: (empty)");
            else
                Debug.Log($"{i}: {s.item.data.ItemName} x{s.item.quantity} (key {s.item.data.key})");
        }
        Debug.Log("=====================");
    }

}
