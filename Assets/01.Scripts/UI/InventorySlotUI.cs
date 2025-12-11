using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InventorySlotUI : MonoBehaviour
{
    [Header("UI 참조")]
    public Image iconImage;                // 아이템 아이콘
    public TextMeshProUGUI amountText;     // 개수 텍스트

    [HideInInspector] public int slotIndex; // 이 UI가 보여줄 인벤토리 인덱스

    public void SetIndex(int index)
    {
        slotIndex = index;
    }

    public void SetSlot(InventorySlot slot)
    {
        if (slot == null || slot.IsEmpty)
        {
            if (iconImage != null)
            {
                iconImage.enabled = false;
                iconImage.sprite = null;
            }

            if (amountText != null)
                amountText.text = "0";
            return;
        }

        if (iconImage != null)
        {
            iconImage.enabled = true;
        }

        if (amountText != null)
        {
            amountText.text = slot.Quantity.ToString();
        }
    }
}
