using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// 인벤토리 한 칸(슬롯)의 UI를 담당하는 스크립트.
/// - 아이콘 / 수량 표시
/// - 좌클릭 시 아이템 액션 팝업 표시
/// </summary>
public class InventorySlotUI : MonoBehaviour,
    IPointerClickHandler,
    IPointerEnterHandler,
    IPointerExitHandler
{
    [Header("UI 참조")]
    public Image iconImage;                  // 슬롯 안의 아이템 아이콘
    public TextMeshProUGUI quantityText;     // 수량 텍스트

    [Header("슬롯 인덱스")]
    public int slotIndex;                    // InventoryManager.slots 에서의 인덱스
    public int SlotIndex => slotIndex;
    /// <summary>InventoryUIController 같은 곳에서 인덱스 세팅할 때 사용</summary>
    public void SetIndex(int index)
    {
        slotIndex = index;
        Refresh();
    }

    /// <summary>
    /// 인벤토리 데이터를 읽어서 이 슬롯의 아이콘/수량 UI를 갱신한다.
    /// </summary>
    public void Refresh()
    {
        var inv = InventoryManager.Instance;
        if (inv == null)
            return;

        var slot = inv.GetSlot(slotIndex);
        if (slot == null || slot.IsEmpty || slot.item == null || slot.item.data == null)
        {
            // 비어있는 슬롯
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
            Sprite iconSprite = null;
            if (!string.IsNullOrEmpty(data.Icon))
            {
                // Resources/ItemIcons/아이콘이름.png
                iconSprite = Resources.Load<Sprite>($"ItemIcons/{data.Icon}");
            }

            if (iconSprite != null)
            {
                iconImage.enabled = true;
                iconImage.sprite = iconSprite;
                iconImage.color = Color.white;
            }
            else
            {
                iconImage.enabled = false;
                iconImage.sprite = null;
            }
        }

        // 수량
        if (quantityText != null)
        {
            int q = itemInstance.quantity;
            if (q > 1)
            {
                quantityText.enabled = true;
                quantityText.text = q.ToString();
            }
            else
            {
                quantityText.enabled = false;
                quantityText.text = "";
            }
        }
    }

    // ─────────────────────────────
    //  마우스 클릭 → 팝업 표시
    // ─────────────────────────────
    public void OnPointerClick(PointerEventData eventData)
    {
        var inv = InventoryManager.Instance;
        if (inv == null)
            return;

        var slot = inv.GetSlot(slotIndex);
        if (slot == null || slot.IsEmpty || slot.item == null)
            return;

        if (eventData.button == PointerEventData.InputButton.Left)
        {
            Debug.Log($"[InventorySlotUI] 슬롯 {slotIndex} 좌클릭 → 팝업 요청");
            if (ItemActionPopupUI.Instance != null)
            {
                ItemActionPopupUI.Instance.Show(slotIndex, slot.item);
            }
        }
    }

    // 지금은 "마우스 올려둘 때 아무 것도 안 보이게"라서 비워둠
    public void OnPointerEnter(PointerEventData eventData) { }

    public void OnPointerExit(PointerEventData eventData) { }
}
