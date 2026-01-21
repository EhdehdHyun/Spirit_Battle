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
        if (slotIndex < 0)
        {
            ClearUI();
            return;
        }

        var inv = InventoryManager.Instance;
        if (inv == null)
        {
            ClearUI();
            return;
        }

        var slot = inv.GetSlot(slotIndex);
        if (slot == null || slot.IsEmpty || slot.item == null || slot.item.data == null)
        {
            ClearUI();
            return;
        }

        var item = slot.item;
        var data = item.data;

        // ===== 아이콘 =====
        if (iconImage != null)
        {
            Sprite sp = ItemIconLoader.Load(data.Icon);

            if (sp != null)
            {
                if (!iconImage.gameObject.activeSelf)
                    iconImage.gameObject.SetActive(true);

                iconImage.enabled = true;
                iconImage.sprite = sp;
                iconImage.color = new Color(1f, 1f, 1f, 1f);
                iconImage.canvasRenderer.SetAlpha(1f);

                iconImage.transform.SetAsLastSibling();

                iconImage.SetAllDirty();
            }
            else
            {
                HideIcon();
            }
        }

        if (quantityText != null)
        {
            if (item.quantity > 1)
            {
                quantityText.enabled = true;
                quantityText.text = item.quantity.ToString();

                // 텍스트도 페이드 잔상 방지
                quantityText.color = new Color(quantityText.color.r, quantityText.color.g, quantityText.color.b, 1f);
                quantityText.canvasRenderer.SetAlpha(1f);
            }
            else
            {
                quantityText.enabled = false;
                quantityText.text = "";
            }
        }
    }

    private void ClearUI()
    {
        HideIcon();

        if (quantityText != null)
        {
            quantityText.text = "";
            quantityText.enabled = false;
            // 혹시 남아있던 알파를 정리
            quantityText.canvasRenderer.SetAlpha(0f);
        }
    }

    private void HideIcon()
    {
        if (iconImage == null) return;
        iconImage.canvasRenderer.SetAlpha(0f);
        iconImage.enabled = false;
        iconImage.sprite = null;
        iconImage.SetAllDirty();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;
        if (slotIndex < 0) return;

        var inv = InventoryManager.Instance;
        if (inv == null) return;

        var slot = inv.GetSlot(slotIndex);
        if (slot == null || slot.IsEmpty || slot.item == null || slot.item.data == null) return;

        if (ItemActionPopupUI.Instance == null)
        {
            return;
        }

        ItemActionPopupUI.Instance.Show(slotIndex, slot.item);
    }
}
