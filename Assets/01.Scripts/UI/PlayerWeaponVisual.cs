using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class WeaponVisualEntry
{
    public int itemKey;          // Data_table.key (예: 3000, 3001 ...)
    public GameObject weaponObj; // 실제 플레이어 손에 붙어있는 무기 오브젝트 (sword2 등)
}

public class PlayerWeaponVisual : MonoBehaviour
{
    [Header("장착 시 보여줄 무기 오브젝트 매핑")]
    public List<WeaponVisualEntry> weaponVisuals = new List<WeaponVisualEntry>();

    private void OnEnable()
    {
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnEquipmentChanged += Refresh;
        }
        Refresh(); // 처음에도 한 번 상태 맞춰주기
    }

    private void OnDisable()
    {
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnEquipmentChanged -= Refresh;
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

        var inv = InventoryManager.Instance;
        if (inv == null || inv.EquippedWeapon == null || inv.EquippedWeapon.data == null)
        {
            // 장비 없음 → 맨손 상태 유지
            return;
        }

        var equippedData = inv.EquippedWeapon.data;
        int key = equippedData.key;

        // 2) 장착된 아이템 key와 같은 무기 오브젝트 찾아서 켜기
        foreach (var entry in weaponVisuals)
        {
            if (entry == null || entry.weaponObj == null)
                continue;

            if (entry.itemKey == key)
            {
                entry.weaponObj.SetActive(true);
                Debug.Log($"[PlayerWeaponVisual] {equippedData.ItemName} (key={key}) 무기 오브젝트 활성화");
                return;
            }
        }

        // 매칭되는 무기 오브젝트 없으면 그냥 맨손 유지
        Debug.Log($"[PlayerWeaponVisual] key={key} 에 해당하는 무기 오브젝트 매핑이 없습니다.");
    }
}
