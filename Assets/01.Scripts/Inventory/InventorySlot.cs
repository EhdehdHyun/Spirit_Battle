using System;
using UnityEngine;

[Serializable]
public class InventorySlot
{
    public ItemInstance item;   // nullÀÌ¸é ºó ½½·Ô

    public bool IsEmpty => item == null || item.quantity <= 0;

    public int ItemKey => IsEmpty ? -1 : item.data.key;
    public int Quantity => IsEmpty ? 0 : item.quantity;

    public void Clear()
    {
        item = null;
    }

    public void Set(ItemInstance newItem)
    {
        item = newItem;
    }

    public bool CanStack(ItemInstance source)
    {
        if (IsEmpty || source == null || source.data == null)
            return false;

        if (item.data.key != source.data.key)
            return false;

        int maxStack = item.data.MaxStack;
        if (maxStack <= 0)
            return false;

        return item.quantity < maxStack;
    }

    public int GetStackSpace()
    {
        if (IsEmpty || item.data == null)
            return 0;

        int maxStack = item.data.MaxStack;
        if (maxStack <= 0)
            return int.MaxValue;

        return Mathf.Max(0, maxStack - item.quantity);
    }
}
