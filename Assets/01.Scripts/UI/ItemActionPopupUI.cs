using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ItemActionPopupUI : MonoBehaviour
{
    public static ItemActionPopupUI Instance { get; private set; }

    [Header("루트 패널")]
    [SerializeField] private GameObject root;      // Panel 전체 (끄고/키는 용도)

    [Header("UI 참조")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI qtyText;
    [SerializeField] private Button useButton;
    [SerializeField] private Button exitButton;

    private int currentSlotIndex = -1;
    private ItemInstance currentItem;

    private void Awake()
    {
        Instance = this;
        Hide();

        if (useButton != null)
            useButton.onClick.AddListener(OnClickUse);

        if (exitButton != null)
            exitButton.onClick.AddListener(OnClickExit);
    }

    public void Show(int slotIndex, ItemInstance item)
    {
        currentSlotIndex = slotIndex;
        currentItem = item;

        if (item != null && item.data != null)
        {
            var data = item.data;

            // 아이콘
            if (iconImage != null)
            {
                Sprite spr = null;
                if (!string.IsNullOrEmpty(data.Icon))
                    spr = Resources.Load<Sprite>($"ItemIcons/{data.Icon}");

                iconImage.sprite = spr;
                iconImage.enabled = (spr != null);
            }

            if (nameText != null)
                nameText.text = data.ItemName;

            if (qtyText != null)
                qtyText.text = $"x{item.quantity}";
        }

        if (root != null)
            root.SetActive(true);

        Debug.Log($"[ItemActionPopupUI] Show slot={slotIndex}, item={currentItem?.data?.ItemName}");
    }

    public void Hide()
    {
        if (root != null)
            root.SetActive(false);

        currentSlotIndex = -1;
        currentItem = null;
    }

    // Use 버튼 → 장비 아이템이면 장착
    private void OnClickUse()
    {
        var inv = InventoryManager.Instance;
        if (inv == null || currentItem == null || currentItem.data == null)
        {
            Hide();
            return;
        }

        var data = currentItem.data;

        if (inv.IsEquipItem(data))
        {
            // 장비 아이템이면 → 해당 슬롯에서 1개 빼고 장비 슬롯에 장착
            inv.EquipFromInventory(currentSlotIndex);
        }
        else
        {
            // 나중에 소비 아이템 사용 로직 넣을 자리
            Debug.Log("[ItemActionPopupUI] 아직 소비/재료 아이템의 Use 로직은 구현하지 않았습니다.");
        }

        RefreshAllInventorySlotsUI();

        Hide();
    }


    // Exit 버튼 → 1개 버리기
    private void OnClickExit()
    {
        var inv = InventoryManager.Instance;
        if (inv != null && currentSlotIndex >= 0)
        {
            inv.DropItemFromSlot(currentSlotIndex, 1);
        }
        Hide();
    }
    private void RefreshAllInventorySlotsUI()
    {
        var allSlotsUI = FindObjectsOfType<InventorySlotUI>(true);

        foreach (var slotUI in allSlotsUI)
        {
            slotUI.Refresh();
        }
    }
}
