using UnityEngine;

public class InventoryUIController : MonoBehaviour
{
    public InventoryManager inventoryManager;
    public InventorySlotUI[] slotUIs;

    private void Awake()
    {
        // 각 슬롯에 자신의 인덱스 알려주기
        for (int i = 0; i < slotUIs.Length; i++)
        {
            if (slotUIs[i] == null) continue;
            slotUIs[i].SetIndex(i);
        }
    }

    private void OnEnable()
    {
        if (inventoryManager != null)
            inventoryManager.OnInventoryChanged += RefreshAll;

        RefreshAll();
    }

    private void OnDisable()
    {
        if (inventoryManager != null)
            inventoryManager.OnInventoryChanged -= RefreshAll;
    }

    public void RefreshAll()
    {
        if (slotUIs == null) return;
        for (int i = 0; i < slotUIs.Length; i++)
        {
            if (slotUIs[i] == null) continue;
            slotUIs[i].Refresh();
        }
    }
}
