using UnityEngine;

public class PlayerWeaponVisual : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private InventoryManager inventoryManager;

    [Header("플레이어 무기 오브젝트")]
    [SerializeField] private GameObject swordObject; // Sword_001

    private void Awake()
    {
        if (inventoryManager == null)
            inventoryManager = InventoryManager.Instance;

        ApplyEquippedVisual("Awake");
    }

    private void OnEnable()
    {
        if (inventoryManager == null)
            inventoryManager = InventoryManager.Instance;

        if (inventoryManager != null)
            inventoryManager.OnInventoryChanged += OnInventoryChanged;

        ApplyEquippedVisual("OnEnable");
    }

    private void OnDisable()
    {
        if (inventoryManager != null)
            inventoryManager.OnInventoryChanged -= OnInventoryChanged;
    }

    private void OnInventoryChanged()
    {
        ApplyEquippedVisual("OnInventoryChanged");
    }

    private void ApplyEquippedVisual(string from)
    {
        if (swordObject == null)
        {
            Debug.LogWarning("[PlayerWeaponVisual] swordObject가 비어있음");
            return;
        }

        if (inventoryManager == null)
            inventoryManager = InventoryManager.Instance;

        if (inventoryManager == null)
        {
            Debug.LogWarning("[PlayerWeaponVisual] InventoryManager.Instance가 없음");
            return;
        }

        // ✅ 슬롯0 직접 확인 (가장 확실)
        var slot0 = inventoryManager.GetSlot(InventoryManager.EquipWeaponIndex);
        bool hasWeaponInSlot0 = (slot0 != null && !slot0.IsEmpty && slot0.item != null && slot0.item.data != null);

        swordObject.SetActive(hasWeaponInSlot0);

        Debug.Log($"[PlayerWeaponVisual:{from}] slot0HasWeapon={hasWeaponInSlot0} " +
                  $"slot0Item={(hasWeaponInSlot0 ? slot0.item.data.ItemName : "null")} " +
                  $"swordActive={swordObject.activeSelf}");
    }
}
