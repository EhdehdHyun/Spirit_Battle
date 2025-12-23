using UnityEngine;

public class PlayerWeaponVisual : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private InventoryManager inventoryManager;
    [SerializeField] private GameObject swordObject;

    [Header("장비 슬롯 인덱스 (InventoryManager 기준)")]
    [SerializeField] private int weaponSlotIndex = 0;   // 무기 장비칸 = 0번

    private void Awake()
    {
        // 시작할 때는 항상 무기 비활성화
        if (swordObject != null)
            swordObject.SetActive(false);
    }

    private void OnEnable()
    {
        if (inventoryManager == null)
            inventoryManager = InventoryManager.Instance;

        if (inventoryManager != null)
            inventoryManager.OnInventoryChanged += RefreshWeaponVisual;

        RefreshWeaponVisual();
    }

    private void OnDisable()
    {
        if (inventoryManager != null)
            inventoryManager.OnInventoryChanged -= RefreshWeaponVisual;
    }

    public void RefreshWeaponVisual()
    {
        if (inventoryManager == null || swordObject == null)
            return;

        var slot = inventoryManager.GetSlot(weaponSlotIndex);

        bool hasWeapon =
            slot != null &&
            !slot.IsEmpty &&
            slot.item != null &&
            slot.item.data != null &&
            inventoryManager.IsEquipItem(slot.item.data);

        Debug.Log($"[PlayerWeaponVisual] RefreshWeaponVisual: idx={weaponSlotIndex}, hasWeapon={hasWeapon}");

        swordObject.SetActive(hasWeapon);
    }
}
