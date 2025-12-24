using UnityEngine;

public class InventoryUIController : MonoBehaviour
{
    public enum PanelType
    {
        WeaponPanel, // 1~25
        ItemPanel,   // 26~50
        EquipPanel   // 0 (1칸)
    }

    [Header("이 패널 타입")]
    [SerializeField] private PanelType panelType = PanelType.WeaponPanel;

    [Header("슬롯 UI 목록 (자식 Slot들에 붙은 InventorySlotUI)")]
    [SerializeField] private InventorySlotUI[] slotUIs;

    [Header("이 패널이 보여줄 InventoryManager 슬롯 범위")]
    [SerializeField] private int startIndex = 0;
    [SerializeField] private int slotCount = 25;

    private void Awake()
    {
        if (slotUIs == null || slotUIs.Length == 0)
            slotUIs = GetComponentsInChildren<InventorySlotUI>(true);

        ApplyPanelRule();
    }

    private void OnEnable()
    {
        ApplyPanelRule();

        if (InventoryManager.Instance != null)
            InventoryManager.Instance.OnInventoryChanged += RefreshAll;

        RefreshAll();
    }

    private void OnDisable()
    {
        if (InventoryManager.Instance != null)
            InventoryManager.Instance.OnInventoryChanged -= RefreshAll;
    }

    private void ApplyPanelRule()
    {
        switch (panelType)
        {
            case PanelType.WeaponPanel:
                startIndex = 1;
                slotCount = 25;
                break;

            case PanelType.ItemPanel:
                startIndex = 26;
                slotCount = 25;
                break;

            case PanelType.EquipPanel:
                startIndex = 0;
                slotCount = 1;
                break;
        }
    }

    public void RefreshAll()
    {
        var inv = InventoryManager.Instance;
        if (inv == null)
        {
            Debug.LogWarning("[InventoryUIController] InventoryManager.Instance가 없습니다.");
            return;
        }

        if (slotUIs == null) return;

        int count = Mathf.Min(slotCount, slotUIs.Length);

        for (int i = 0; i < count; i++)
        {
            int inventoryIndex = startIndex + i;

            if (inventoryIndex < 0 || inventoryIndex >= inv.slots.Count)
            {
                slotUIs[i].SetIndex(-1);
                continue;
            }

            slotUIs[i].SetIndex(inventoryIndex);
        }

        for (int i = count; i < slotUIs.Length; i++)
            slotUIs[i].SetIndex(-1);
    }
}
