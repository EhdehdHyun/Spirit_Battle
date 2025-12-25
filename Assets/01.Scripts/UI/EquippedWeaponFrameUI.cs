using UnityEngine;
using UnityEngine.UI;

public class EquippedWeaponFrameUI : MonoBehaviour
{
    [SerializeField] private Image iconImage;        // EquipmentIconFrame_Weapon/Icon
    [SerializeField] private GameObject emptyIcon;   // EquipmentIconFrame_Weapon/EmptyIcon

    private InventoryManager inv;

    private void OnEnable()
    {
        inv = InventoryManager.Instance;
        if (inv != null) inv.OnInventoryChanged += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        if (inv != null) inv.OnInventoryChanged -= Refresh;
    }

    private Sprite LoadIconSprite(string iconKey)
    {
        if (string.IsNullOrEmpty(iconKey)) return null;

        // ✅ 1) 기존 방식
        Sprite sp = Resources.Load<Sprite>($"ItemIcons/{iconKey}");

        // ✅ 2) B방법 폴더 구조
        if (sp == null)
            sp = Resources.Load<Sprite>($"ItemIcons/{iconKey}/{iconKey}");

        return sp;
    }

    public void Refresh()
    {
        if (inv == null) return;

        var slot0 = inv.GetSlot(InventoryManager.EquipWeaponIndex); // 0
        var item = slot0 != null ? slot0.item : null;

        if (item == null || item.data == null)
        {
            if (iconImage != null) { iconImage.enabled = false; iconImage.sprite = null; }
            if (emptyIcon != null) emptyIcon.SetActive(true);
            return;
        }

        Sprite sp = null;
        if (!string.IsNullOrEmpty(item.data.Icon))
            sp = ItemIconLoader.Load(item.data.Icon);

        if (sp == null && !string.IsNullOrEmpty(item.data.Icon))
            Debug.LogWarning($"[EquippedWeaponFrameUI] 아이콘 로드 실패: Icon='{item.data.Icon}' " +
                             $"Try: 'Resources/ItemIcons/{item.data.Icon}' OR 'Resources/ItemIcons/{item.data.Icon}/{item.data.Icon}'");

        if (iconImage != null)
        {
            iconImage.sprite = sp;
            iconImage.enabled = (sp != null);
        }

        if (emptyIcon != null)
            emptyIcon.SetActive(sp == null);

        Debug.Log($"[EquippedWeaponFrameUI] slot0={item.data.ItemName}, icon='{item.data.Icon}'");
    }
}
