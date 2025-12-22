using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// 왼쪽 장비 슬롯(칼+도끼 아이콘 자리) UI
/// - 장착된 무기 아이콘 표시
/// - 우클릭 시 장비 해제 + 인벤토리로 되돌리기
/// </summary>
public class EquipmentSlotUI : MonoBehaviour, IPointerClickHandler
{
    [Header("UI 참조")]
    [SerializeField] private Image iconImage;          // 장비칸 아이콘 이미지
    [SerializeField] private TextMeshProUGUI nameText; // 선택사항: 이름 표시용 (없으면 비워둬도 됨)

    private void OnEnable()
    {
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnEquipmentChanged += Refresh;
        }
        Refresh();
    }

    private void OnDisable()
    {
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnEquipmentChanged -= Refresh;
        }
    }

    /// <summary>
    /// 현재 EquippedWeapon 정보를 읽어서 아이콘/이름 갱신
    /// </summary>
    public void Refresh()
    {
        var inv = InventoryManager.Instance;
        if (inv == null || inv.EquippedWeapon == null || inv.EquippedWeapon.data == null)
        {
            // 장비 없음 → 아이콘 숨기기
            if (iconImage != null)
            {
                iconImage.enabled = false;
                iconImage.sprite = null;
            }
            if (nameText != null)
            {
                nameText.text = "";
            }
            return;
        }

        var item = inv.EquippedWeapon;
        var data = item.data;

        // 아이콘 세팅
        if (iconImage != null)
        {
            Sprite spr = null;
            if (!string.IsNullOrEmpty(data.Icon))
            {
                spr = Resources.Load<Sprite>($"ItemIcons/{data.Icon}");
            }

            iconImage.sprite = spr;
            iconImage.enabled = (spr != null);

            if (spr != null)
                iconImage.color = Color.white;
        }

        // 이름 텍스트 (원하면 사용)
        if (nameText != null)
        {
            nameText.text = data.ItemName;
        }
    }

    /// <summary>
    /// 장비칸 우클릭 → 장비 해제
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            Debug.Log("[EquipmentSlotUI] 우클릭 → 장비 해제 시도");
            InventoryManager.Instance?.UnequipWeaponToInventory();
        }
    }
}
