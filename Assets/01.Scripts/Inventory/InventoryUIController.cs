using UnityEngine;

public class InventoryUIController : MonoBehaviour
{
    [Header("슬롯 UI 목록 (자식 Slot들에 붙은 InventorySlotUI)")]
    [SerializeField] private InventorySlotUI[] slotUIs;

    [Header("이 패널이 보여줄 InventoryManager 슬롯 범위")]
    [SerializeField] private int startIndex = 0;
    [SerializeField] private int slotCount = 25;

    private void Awake()
    {
        // 인스펙터에 안 넣었으면 자식에서 자동 수집
        if (slotUIs == null || slotUIs.Length == 0)
            slotUIs = GetComponentsInChildren<InventorySlotUI>(true);
    }

    private void OnEnable()
    {
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnInventoryChanged += RefreshAll;
        }

        RefreshAll();
    }

    private void OnDisable()
    {
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnInventoryChanged -= RefreshAll;
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

        // 이 패널에 배치된 UI 슬롯 수와 slotCount가 다르면,
        // 우선 UI 슬롯 개수를 기준으로 안전하게 처리
        int count = Mathf.Min(slotCount, slotUIs.Length);

        for (int i = 0; i < count; i++)
        {
            int inventoryIndex = startIndex + i;
            slotUIs[i].SetIndex(inventoryIndex);
            // SetIndex 내부에서 Refresh()가 호출되게 해둔 상태면 추가 호출 필요 없음
            // 만약 SetIndex가 인덱스만 넣는다면, slotUIs[i].Refresh(); 호출 추가
        }

        // UI 슬롯이 더 많은 경우, 남는 슬롯들은 비워주기
        for (int i = count; i < slotUIs.Length; i++)
        {
            slotUIs[i].SetIndex(-1); // -1이면 빈 슬롯 처리하도록 InventorySlotUI에서 처리
        }
    }
}
