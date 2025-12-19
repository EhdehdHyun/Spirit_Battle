using UnityEngine;

/// <summary>
/// 인벤토리 패널(WeaponPanel / ItemPanel 각각)에 붙는 컨트롤러.
/// - 이 패널이 담당할 InventorySlotUI들을 묶어서
///   InventoryManager.slots의 특정 구간(startIndex ~)과 연결해 준다.
/// - InventoryManager.OnInventoryChanged 이벤트를 구독해서
///   인벤토리가 바뀔 때마다 슬롯 UI를 새로 그려준다.
/// </summary>
public class InventoryUIController : MonoBehaviour
{
    [Header("참조")]
    [Tooltip("인벤토리 데이터 매니저 (비우면 자동으로 InventoryManager.Instance를 사용)")]
    [SerializeField] private InventoryManager inventoryManager;

    [Header("이 패널이 관리할 슬롯 UI들")]
    [Tooltip("이 패널 아래에 있는 InventorySlotUI들을 모두 넣어주세요 (순서 중요).")]
    [SerializeField] private InventorySlotUI[] slotUIs;

    [Header("InventoryManager.slots 시작 인덱스")]
    [Tooltip("이 패널이 보여줄 첫 슬롯 인덱스 (예: 장비창은 0, 재료창은 equipSlotCount).")]
    [SerializeField] private int startIndex = 0;

    private bool _initialized = false;

    private void Awake()
    {
        // inventoryManager를 지정 안 해놨으면 Instance 자동 참조
        if (inventoryManager == null)
            inventoryManager = InventoryManager.Instance;
    }

    private void OnEnable()
    {
        TryInitialize();
        RefreshAll();
    }

    private void Start()
    {
        // 혹시 OnEnable 전에 Start가 먼저 도는 경우 대비
        TryInitialize();
        RefreshAll();
    }

    private void OnDisable()
    {
        // 이벤트 구독 해제
        if (inventoryManager != null)
            inventoryManager.OnInventoryChanged -= RefreshAll;
    }

    /// <summary>
    /// InventoryManager 이벤트 구독 및 슬롯 인덱스 설정
    /// </summary>
    private void TryInitialize()
    {
        if (_initialized)
            return;

        if (inventoryManager == null)
        {
            inventoryManager = InventoryManager.Instance;
            if (inventoryManager == null)
            {
                Debug.LogWarning("[InventoryUIController] InventoryManager.Instance 를 찾지 못했습니다.");
                return;
            }
        }

        if (slotUIs == null || slotUIs.Length == 0)
        {
            Debug.LogWarning("[InventoryUIController] slotUIs 가 설정되어 있지 않습니다.");
            return;
        }

        // 슬롯 UI들에게 자기 글로벌 인덱스를 세팅
        for (int i = 0; i < slotUIs.Length; i++)
        {
            if (slotUIs[i] == null) continue;
            slotUIs[i].SetIndex(startIndex + i);
        }

        // 인벤토리 변경 이벤트 구독
        inventoryManager.OnInventoryChanged -= RefreshAll;
        inventoryManager.OnInventoryChanged += RefreshAll;

        _initialized = true;
    }

    /// <summary>
    /// 이 패널이 관리하는 모든 슬롯 UI를 새로 고침
    /// </summary>
    public void RefreshAll()
    {
        if (slotUIs == null) return;

        foreach (var ui in slotUIs)
        {
            if (ui != null)
                ui.Refresh();
        }
    }
}
