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

    [Header("✅ Body(설명) - Description만 출력")]
    [SerializeField] private TextMeshProUGUI bodyText; // Panel/Body 연결

    [Header("버튼들")]
    [SerializeField] private Button useButton;      // 사용
    [SerializeField] private Button dropButton;     // 버리기
    [SerializeField] private Button closeButton;    // 우측상단 X 닫기

    private int _slotIndex = -1;
    private ItemInstance _cachedItem;

    public bool IsOpen => root != null && root.activeSelf;

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

        if (dropButton != null)
            dropButton.onClick.AddListener(OnClickDrop);

        if (closeButton != null)
            closeButton.onClick.AddListener(OnClickClose);
    }

    private void Update()
    {
        // ✅ ESC 누르면 툴팁 닫기
        if (IsOpen && Input.GetKeyDown(KeyCode.Escape))
            Hide();
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

        var data = item.data;

        // 아이콘 로드(Resources)
        if (iconImage != null)
        {
            Sprite icon = null;
            if (!string.IsNullOrEmpty(data.Icon))
            {
                icon = Resources.Load<Sprite>($"ItemIcons/{data.Icon}");
                if (icon == null)
                    icon = Resources.Load<Sprite>($"ItemIcons/{data.Icon}/{data.Icon}");
            }

            iconImage.sprite = icon;
            iconImage.enabled = (icon != null);
        }

        if (nameText != null)
            nameText.text = data.ItemName;

        // ✅ Body에는 Description만 출력
        if (bodyText != null)
        {
            // Description이 비어있으면 그냥 빈 문자열
            bodyText.text = string.IsNullOrEmpty(data.Description) ? "" : data.Description;
        }
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
        if (inv == null) return;

        if (_slotIndex < 0)
        {
            Hide();
            return;
        }

        var slot = inv.GetSlot(_slotIndex);
        if (slot == null || slot.IsEmpty || slot.item == null || slot.item.data == null)
        {
            Hide();
            return;
        }

        var data = slot.item.data;

        // 장비면 장착
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

        Hide();
    }

    // 버리기 버튼
    public void OnClickDrop()
    {
        var inv = InventoryManager.Instance;
        if (inv == null) return;

        if (_slotIndex < 0)
        {
            Hide();
            return;
        }

        inv.DropItemFromSlot(_slotIndex, 1);
        Hide();
    }

    // 우측상단 닫기(X)
    public void OnClickClose()
    {
        Hide();
    }
}
