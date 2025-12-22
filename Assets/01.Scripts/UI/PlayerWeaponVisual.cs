using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class WeaponVisualEntry
{
    [Tooltip("이 무기와 연결될 아이템 키 (Data_table.key)")]
    public int itemKey;          // 예: 3000, 3001 ...

    [Tooltip("플레이어 손에 붙어있는 무기 오브젝트 (Sword_001 같은 것)")]
    public GameObject weaponObj; // 예: Player/root/Hips/.../Weapon_Target_H/Sword_001
}

public class PlayerWeaponVisual : MonoBehaviour
{
    [Header("인벤토리 매니저 (빈칸이면 자동으로 Instance 사용)")]
    public InventoryManager inventoryManager;

    [Header("아이템 키 ↔ 무기 오브젝트 매핑")]
    public List<WeaponVisualEntry> weaponVisuals = new List<WeaponVisualEntry>();

    private void Awake()
    {
        if (inventoryManager == null)
            inventoryManager = InventoryManager.Instance;
    }

    private void OnEnable()
    {
        if (inventoryManager != null)
        {
            inventoryManager.OnEquipmentChanged += Refresh;
        }

        // 처음 켰을 때도 현재 장착 상태에 맞춰서 한 번 동기화
        Refresh();
    }

    private void OnDisable()
    {
        if (inventoryManager != null)
        {
            inventoryManager.OnEquipmentChanged -= Refresh;
        }
    }

    /// <summary>
    /// 현재 EquippedWeapon 데이터를 보고
    /// 해당하는 무기 오브젝트만 활성화, 나머지는 비활성화
    /// </summary>
    public void Refresh()
    {
        // 1) 일단 모든 무기 오브젝트 끄기 (맨손 상태)
        foreach (var entry in weaponVisuals)
        {
            if (entry != null && entry.weaponObj != null)
            {
                entry.weaponObj.SetActive(false);
            }
        }

        if (inventoryManager == null ||
            inventoryManager.EquippedWeapon == null ||
            inventoryManager.EquippedWeapon.data == null)
        {
            // 장비가 없으면 그냥 맨손 유지
            return;
        }

        int equippedKey = inventoryManager.EquippedWeapon.data.key;

        // 2) 장착된 아이템 key와 같은 무기 오브젝트 찾아서 켜기
        foreach (var entry in weaponVisuals)
        {
            if (entry == null || entry.weaponObj == null)
                continue;

            if (entry.itemKey == equippedKey)
            {
                entry.weaponObj.SetActive(true);
                Debug.Log($"[PlayerWeaponVisual] key={equippedKey} 무기 오브젝트 활성화: {entry.weaponObj.name}");
                return;
            }
        }

        Debug.Log($"[PlayerWeaponVisual] key={equippedKey} 에 해당하는 무기 오브젝트 매핑이 없습니다.");
    }
}
