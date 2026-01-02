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
                // ✅ (중요) 오브젝트/컴포넌트 켜기
                if (!iconImage.gameObject.activeSelf)
                    iconImage.gameObject.SetActive(true);

                iconImage.enabled = true;
                iconImage.sprite = sp;
                iconImage.color = new Color(1f, 1f, 1f, 1f);

                // ✅ (핵심) CanvasRenderer 알파가 0으로 남아있을 수 있어서 강제 복구
                iconImage.canvasRenderer.SetAlpha(1f);

                // ✅ 혹시 형제에 가려지면 위로
                iconImage.transform.SetAsLastSibling();

                // 강제 갱신
                iconImage.SetAllDirty();
            }
            else
            {
                Debug.LogWarning($"[InventorySlotUI] icon load FAIL: slotIndex={slotIndex}, item={data.ItemName}, Icon='{data.Icon}'");
                HideIcon();
            }
        }

        // ===== 수량 =====
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

        // ✅ 페이드 잔상 방지: 알파도 같이 0으로
        iconImage.canvasRenderer.SetAlpha(0f);
        iconImage.enabled = false;
        iconImage.sprite = null;
        iconImage.SetAllDirty();

        // 원하면 빈칸일 때 GO 자체도 꺼도 됨
        // iconImage.gameObject.SetActive(false);
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
            Debug.LogWarning("[InventorySlotUI] ItemActionPopupUI.Instance is null");
            return;
        }

        ItemActionPopupUI.Instance.Show(slotIndex, slot.item);
    }
}
