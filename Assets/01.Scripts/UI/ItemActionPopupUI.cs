using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ItemActionPopupUI : MonoBehaviour
{
    public static ItemActionPopupUI Instance { get; private set; }

    [Header("루트 패널")]
    [SerializeField] private GameObject root;

    [Header("UI 참조")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI qtyText;
    [SerializeField] private Button useButton;
    [SerializeField] private Button exitButton;

    private int _slotIndex = -1;
    private ItemInstance _cachedItem;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (root != null)
            root.SetActive(false);

        if (useButton != null)
            useButton.onClick.AddListener(OnClickUse);

        if (exitButton != null)
            exitButton.onClick.AddListener(OnClickExit);
    }

    private Sprite LoadIconSprite(string iconKey)
    {
        if (string.IsNullOrEmpty(iconKey)) return null;

        // ✅ 1) 기존 방식
        Sprite sp = Resources.Load<Sprite>($"ItemIcons/{iconKey}");

        // ✅ 2) B방법 폴더 구조
        if (sp == null)
            sp = Resources.Load<Sprite>($"ItemIcons/{iconKey}/{iconKey}");

        return sp;
    }

    public void Show(int slotIndex, ItemInstance item)
    {
        _slotIndex = slotIndex;
        _cachedItem = item;

        if (root != null)
        {
            root.SetActive(true);
            root.transform.SetAsLastSibling();
        }

        if (item == null || item.data == null)
        {
            Debug.LogWarning("[ItemActionPopupUI] Show: item/data null");
            return;
        }

        // 아이콘 로드
        if (iconImage != null)
        {
            Sprite icon = null;
            if (!string.IsNullOrEmpty(item.data.Icon))
                icon = ItemIconLoader.Load(item.data.Icon);

            if (icon == null && !string.IsNullOrEmpty(item.data.Icon))
                Debug.LogWarning($"[ItemActionPopupUI] 아이콘 로드 실패: Icon='{item.data.Icon}' " +
                                 $"Try: 'Resources/ItemIcons/{item.data.Icon}' OR 'Resources/ItemIcons/{item.data.Icon}/{item.data.Icon}'");

            iconImage.sprite = icon;
            iconImage.enabled = (icon != null);
        }

        if (nameText != null)
            nameText.text = item.data.ItemName;

        if (qtyText != null)
            qtyText.text = $"보유 {item.quantity}";

        Debug.Log($"[ItemActionPopupUI] Show slot={slotIndex}, item={item.data.ItemName}");
    }

    public void Hide()
    {
        if (root != null)
            root.SetActive(false);

        _slotIndex = -1;
        _cachedItem = null;
    }

    // ===============
    // Use 버튼
    // ===============
    public void OnClickUse()
    {
        var inv = InventoryManager.Instance;
        if (inv == null)
        {
            Debug.LogWarning("[ItemActionPopupUI] OnClickUse: InventoryManager null");
            return;
        }

        if (_slotIndex < 0)
            return;

        var slot = inv.GetSlot(_slotIndex);
        if (slot == null || slot.IsEmpty || slot.item == null || slot.item.data == null)
        {
            Hide();
            return;
        }

        var data = slot.item.data;

        // ✅ 장비칸(0번)에서 Use를 누르면 = 해제
        if (_slotIndex == InventoryManager.EquipWeaponIndex)
        {
            inv.UnequipWeapon();
            Hide();
            return;
        }

        // 장비면 장착 (WeaponPanel 범위에서만 통과)
        if (inv.IsEquipItem(data))
        {
            inv.EquipFromInventory(_slotIndex);
            Hide();
            return;
        }

        // 소비템이면 사용
        if (inv.IsConsumableItem(data))
        {
            inv.UseItemFromSlot(_slotIndex, 1);
            Hide();
            return;
        }

        Debug.Log("[ItemActionPopupUI] OnClickUse: 처리할 타입이 아님");
        Hide();
    }

    public void OnClickExit()
    {
        var inv = InventoryManager.Instance;
        if (inv == null)
        {
            Debug.LogWarning("[ItemActionPopupUI] OnClickExit: InventoryManager null");
            return;
        }

        if (_slotIndex < 0)
            return;

        inv.DropItemFromSlot(_slotIndex, 1);
        Hide();
    }
}
