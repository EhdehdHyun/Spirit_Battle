using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class InventorySlotUI : MonoBehaviour,
    IPointerClickHandler,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerMoveHandler
{
    [Header("UI 참조")]
    public Image iconImage;
    public TextMeshProUGUI amountText;

    [HideInInspector] public int slotIndex;

    // 현재 슬롯이 바라보고 있는 데이터
    private InventorySlot _slotData;
    private ItemInstance _currentItem;

    // ====== 슬롯 인덱스 세팅 (InventoryUIController에서 호출) ======
    public void SetIndex(int index)
    {
        slotIndex = index;
    }

    // ====== 슬롯 데이터 세팅 (InventoryUIController에서 호출) ======
    public void SetSlot(InventorySlot slot)
    {
        _slotData = slot;

        if (iconImage == null || amountText == null)
            return;

        // 비어 있는 슬롯
        if (slot == null || slot.IsEmpty)
        {
            _currentItem = null;
            iconImage.enabled = false;
            iconImage.sprite = null;
            amountText.text = "0";
            return;
        }

        _currentItem = slot.item;

        var itemInstance = slot.item;
        var data = itemInstance.data;
        int quantity = itemInstance.quantity;

        // 아이콘 로드 (Resources/ItemIcons/아이콘이름)
        Sprite iconSprite = null;
        if (!string.IsNullOrEmpty(data.Icon))
        {
            iconSprite = Resources.Load<Sprite>($"ItemIcons/{data.Icon}");
        }

        if (iconSprite != null)
        {
            iconImage.enabled = true;
            iconImage.sprite = iconSprite;
        }
        else
        {
            iconImage.enabled = false;
            iconImage.sprite = null;
        }

        amountText.text = quantity.ToString();
    }

    // 인벤토리 데이터가 바뀌었을 때 다시 그릴 수 있도록
    public void Refresh()
    {
        if (InventoryManager.Instance == null) return;
        var slot = InventoryManager.Instance.GetSlot(slotIndex);
        SetSlot(slot);
    }

    // ====== 클릭해서 아이템 버리기 (이미 사용 중이던 기능) ======
    public void OnPointerClick(PointerEventData eventData)
    {
        // 왼쪽 클릭일 때만
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        if (InventoryManager.Instance == null)
            return;

        // 슬롯이 비어있으면 아무 것도 안 함
        if (_slotData == null || _slotData.IsEmpty)
            return;

        // 1개 버리기
        InventoryManager.Instance.DropItemFromSlot(slotIndex, 1);

        // 데이터/UI 갱신
        Refresh();

        // 아이템이 사라졌다면 툴팁도 같이 숨김
        if (_slotData == null || _slotData.IsEmpty)
        {
            if (ItemTooltipUI.Instance != null)
                ItemTooltipUI.Instance.Hide();
        }
    }

    // ====== 마우스가 슬롯 안으로 들어왔을 때 ======
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_currentItem == null || _currentItem.data == null)
            return;

        if (ItemTooltipUI.Instance != null)
        {
            ItemTooltipUI.Instance.Show(_currentItem, eventData.position);
        }
    }

    // ====== 마우스가 슬롯 밖으로 나갔을 때 ======
    public void OnPointerExit(PointerEventData eventData)
    {
        if (ItemTooltipUI.Instance != null)
        {
            ItemTooltipUI.Instance.Hide();
        }
    }

    // ====== 마우스가 슬롯 안에서 움직일 때 (툴팁 따라 다니게) ======
    public void OnPointerMove(PointerEventData eventData)
    {
        if (ItemTooltipUI.Instance != null && _currentItem != null)
        {
            ItemTooltipUI.Instance.UpdatePosition(eventData.position);
        }
    }
}
