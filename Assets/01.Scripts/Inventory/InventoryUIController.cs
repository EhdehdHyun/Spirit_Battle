using System.Collections.Generic;
using UnityEngine;

public class InventoryUIController : MonoBehaviour
{
    public InventoryManager inventory;
    public List<InventorySlotUI> slotUIs = new List<InventorySlotUI>();

    private void Awake()
    {
        if (inventory == null)
            inventory = InventoryManager.Instance;

        // 인덱스 자동 세팅
        for (int i = 0; i < slotUIs.Count; i++)
        {
            if (slotUIs[i] != null)
                slotUIs[i].SetIndex(i);
        }
    }

    private void OnEnable()
    {
        if (inventory == null)
            inventory = InventoryManager.Instance;

        if (inventory != null)
            inventory.OnInventoryChanged += RefreshAll;

        RefreshAll();
    }

    private void OnDisable()
    {
        if (inventory != null)
            inventory.OnInventoryChanged -= RefreshAll;
    }

    public void RefreshAll()
    {
        if (inventory == null) return;
        var slots = inventory.slots;

        for (int i = 0; i < slotUIs.Count; i++)
        {
            var ui = slotUIs[i];
            if (ui == null) continue;

            if (i < slots.Count)
                ui.SetSlot(slots[i]);
            else
                ui.SetSlot(null);
        }
    }
}
