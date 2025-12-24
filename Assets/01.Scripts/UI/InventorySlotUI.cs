using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventorySlotUI : MonoBehaviour, IPointerClickHandler
{
    [Header("UI Ref")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI quantityText;

    [Header("Slot Index (InventoryManager.slots)")]
    [SerializeField] private int slotIndex;

    public void SetIndex(int index)
    {
        slotIndex = index;
        Refresh();
    }

    public int GetIndex() => slotIndex;

    public void Refresh()
    {
        var inv = InventoryManager.Instance;
        if (inv == null) return;

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

        var item = slot.item;
        var data = item.data;

        // 아이콘 로드(Resources/ItemIcons/아이콘명)
        if (iconImage != null)
        {
            Sprite sp = null;
            if (!string.IsNullOrEmpty(data.Icon))
                sp = Resources.Load<Sprite>($"ItemIcons/{data.Icon}");

            if (sp != null)
            {
                iconImage.enabled = true;
                iconImage.sprite = sp;
                iconImage.color = Color.white;
            }
            else
            {
                iconImage.enabled = false;
                iconImage.sprite = null;
            }
        }

        // 수량 표시(1이면 숨김)
        if (quantityText != null)
        {
            if (item.quantity > 1)
            {
                quantityText.enabled = true;
                quantityText.text = item.quantity.ToString();
            }
            else
            {
                quantityText.enabled = false;
                quantityText.text = "";
            }
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;

        var inv = InventoryManager.Instance;
        if (inv == null) return;

        var slot = inv.GetSlot(slotIndex);
        if (slot == null || slot.IsEmpty || slot.item == null) return;

        if (ItemActionPopupUI.Instance == null)
        {
            Debug.LogWarning("[InventorySlotUI] ItemActionPopupUI.Instance is null");
            return;
        }

        Debug.Log($"[InventorySlotUI] Click slot={slotIndex} popup!");
        ItemActionPopupUI.Instance.Show(slotIndex, slot.item);
    }
}
