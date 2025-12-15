using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class InventorySlotUI : MonoBehaviour, IPointerClickHandler
{
    [Header("UI 참조")]
    public Image iconImage;                     // 아이템 아이콘
    public TextMeshProUGUI amountText;         // 개수 텍스트

    [HideInInspector] public int slotIndex;    // 이 슬롯이 몇 번째 인벤 슬롯인지

    // InventoryUIController 에서 인덱스 세팅할 때 호출
    public void SetIndex(int index)
    {
        slotIndex = index;
    }

    // InventoryUIController 에서 슬롯 데이터 갱신할 때 호출
    public void SetSlot(InventorySlot slot)
    {
        if (iconImage == null || amountText == null)
            return;

        // 비어있는 슬롯
        if (slot == null || slot.IsEmpty)
        {
            iconImage.enabled = false;
            iconImage.sprite = null;
            amountText.text = "0";
            return;
        }

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

    public void Refresh()
    {
        if (InventoryManager.Instance == null) return;
        var slot = InventoryManager.Instance.GetSlot(slotIndex);
        SetSlot(slot);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // 왼쪽 클릭일 때만
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        if (InventoryManager.Instance == null)
            return;

        // 이 슬롯에서 1개 버리기
        InventoryManager.Instance.DropItemFromSlot(slotIndex, 1);

        // 데이터가 바뀌었으니 UI 갱신
        Refresh();
    }
}
