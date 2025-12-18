using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class InventorySlotUI : MonoBehaviour,
    IPointerClickHandler,
    IPointerEnterHandler,
    IPointerExitHandler
{
    [Header("UI 참조")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI quantityText;

    [Header("슬롯 인덱스")]
    [SerializeField] private int slotIndex;

    public void SetIndex(int index)
    {
        slotIndex = index;
        Refresh();
    }

    public void Refresh()
    {
        var inv = InventoryManager.Instance;
        if (inv == null)
        {
            Debug.LogWarning("[InventorySlotUI] InventoryManager.Instance 가 없습니다.");
            return;
        }

        var slot = inv.GetSlot(slotIndex);
        if (slot == null || slot.IsEmpty || slot.item == null || slot.item.data == null)
        {
            if (iconImage != null)
            {
                iconImage.enabled = false;
                iconImage.sprite = null;
            }

            if (quantityText != null)
            {
                quantityText.text = "";
                quantityText.enabled = false;
            }
            return;
        }

        var itemInstance = slot.item;
        var data = itemInstance.data;

        // 아이콘
        if (iconImage != null)
        {
            iconImage.enabled = true;
            Sprite iconSprite = null;

            if (!string.IsNullOrEmpty(data.Icon))
                iconSprite = Resources.Load<Sprite>($"ItemIcons/{data.Icon}");

            if (iconSprite != null)
            {
                iconImage.sprite = iconSprite;
                iconImage.color = Color.white;
            }
            else
            {
                Debug.LogWarning($"[InventorySlotUI] 아이콘 스프라이트를 찾지 못했습니다. Icon={data.Icon}, slotIndex={slotIndex}");
                iconImage.enabled = false;
            }
        }

        // 수량
        if (quantityText != null)
        {
            int q = itemInstance.quantity;
            if (q > 1)
            {
                quantityText.text = q.ToString();
                quantityText.enabled = true;
            }
            else
            {
                quantityText.text = "";
                quantityText.enabled = false;
            }
        }
    }

    // ▼ 아이템 버리기 클릭
    public void OnPointerClick(PointerEventData eventData)
    {
        var inv = InventoryManager.Instance;
        if (inv == null)
            return;

        var slot = inv.GetSlot(slotIndex);
        if (slot == null || slot.IsEmpty)
            return;

        if (eventData.button == PointerEventData.InputButton.Left)
        {
            Debug.Log($"[InventorySlotUI] 슬롯 {slotIndex} 좌클릭 → DropItemFromSlot 호출");
            inv.DropItemFromSlot(slotIndex, 1);
        }
    }

    // ▼ 마우스 올렸을 때 툴팁
    public void OnPointerEnter(PointerEventData eventData)
    {
        var inv = InventoryManager.Instance;
        if (inv == null)
            return;

        var slot = inv.GetSlot(slotIndex);
        if (slot == null || slot.IsEmpty || slot.item == null || slot.item.data == null)
            return;

        var itemInstance = slot.item;
        var data = itemInstance.data;

        Debug.Log($"[InventorySlotUI] OnPointerEnter slot {slotIndex}, item={data.ItemName}");

        if (ItemTooltipUI.Instance != null)
        {
            Debug.Log("[InventorySlotUI] ItemTooltipUI.Instance.Show 호출");
            ItemTooltipUI.Instance.Show(data, itemInstance.quantity);
        }
        else
        {
            Debug.LogWarning("[InventorySlotUI] ItemTooltipUI.Instance 가 null 입니다.");
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (ItemTooltipUI.Instance != null)
        {
            ItemTooltipUI.Instance.Hide();
        }
    }
}
