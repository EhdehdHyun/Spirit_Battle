using UnityEngine;
using UnityEngine.UI;

public class InventoryTabController : MonoBehaviour
{
    [Header("탭 패널들")]
    [SerializeField] private GameObject weaponPanel; // 장비 인벤토리 패널
    [SerializeField] private GameObject itemPanel;   // 재료/소비 인벤토리 패널

    [Header("탭 버튼 (선택 색 변경용, 옵션)")]
    [SerializeField] private Image weaponTabButtonImage; // Icon_1 의 Image
    [SerializeField] private Image itemTabButtonImage;   // Icon_2 의 Image
    [SerializeField] private Color selectedColor = Color.white;
    [SerializeField] private Color normalColor = Color.gray;

    private void Awake()
    {
        // 시작할 때 장비 탭이 기본으로 열리게
        ShowWeaponTab();
    }

    public void ShowWeaponTab()
    {
        Debug.Log("[InventoryTabController] ShowWeaponTab");

        SetActiveSafe(weaponPanel, true);
        SetActiveSafe(itemPanel, false);

        UpdateTabColors(isWeaponTab: true);
    }

    public void ShowItemTab()
    {
        Debug.Log("[InventoryTabController] ShowItemTab");

        SetActiveSafe(weaponPanel, false);
        SetActiveSafe(itemPanel, true);

        UpdateTabColors(isWeaponTab: false);
    }

    private void SetActiveSafe(GameObject go, bool active)
    {
        if (go == null) return;
        if (go.activeSelf == active) return;
        go.SetActive(active);
    }

    private void UpdateTabColors(bool isWeaponTab)
    {
        if (weaponTabButtonImage != null)
            weaponTabButtonImage.color = isWeaponTab ? selectedColor : normalColor;

        if (itemTabButtonImage != null)
            itemTabButtonImage.color = isWeaponTab ? normalColor : selectedColor;
    }
}
