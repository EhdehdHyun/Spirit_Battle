using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class DropPrefabEntry
{
    public int itemKey;
    public GameObject prefab;
}

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    // =========================================================
    // ✅ 슬롯 인덱스 규칙 (고정)
    //  - 장비칸(무기 장착): 0
    //  - WeaponPanel(무기 인벤): 1 ~ 25 (25칸)
    //  - ItemPanel(아이템 인벤): 26 ~ 50 (25칸)
    //  - 총 슬롯: 0 ~ 50 (51개)
    // =========================================================
    public const int EquipWeaponIndex = 0;

    public const int WeaponInvStart = 1;
    public const int WeaponInvCount = 25;                      // 1~25
    public const int WeaponInvEnd = WeaponInvStart + WeaponInvCount - 1;

    public const int ItemInvStart = 26;
    public const int ItemInvCount = 25;                        // 26~50
    public const int ItemInvEnd = ItemInvStart + ItemInvCount - 1;

    public const int TotalSlotCount = 51;                      // 0~50

    [Header("Grid Size (Rows x Columns) - (기존 변수 유지용, 실제 슬롯 수는 TotalSlotCount로 고정)")]
    public int rows = 5;
    public int columns = 5;

    [Tooltip("인벤토리 슬롯 리스트 (총 51개: 0~50)")]
    public List<InventorySlot> slots = new List<InventorySlot>();

    [Header("참조")]
    [Tooltip("플레이어 상호작용 스크립트 (플레이어 위치/방향 얻기용)")]
    public PlayerInteraction playerInteraction;

    [Header("드랍 프리팹 매핑")]
    public List<DropPrefabEntry> dropPrefabs = new List<DropPrefabEntry>();
    public GameObject defaultDropPrefab;

    [Header("장착 슬롯 인덱스(고정)")]
    public int WeaponEquipStartIndex = EquipWeaponIndex;

    /// <summary>인벤토리가 바뀔 때마다 UI가 구독하는 이벤트</summary>
    public event Action OnInventoryChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        WeaponEquipStartIndex = EquipWeaponIndex;

        InitSlots();
        Debug.Log($"[InventoryManager] Awake: slot count = {slots.Count} (expected {TotalSlotCount})");
    }

    private void InitSlots()
    {
        int total = TotalSlotCount;

        if (slots == null)
            slots = new List<InventorySlot>(total);

        while (slots.Count < total)
            slots.Add(new InventorySlot());

        if (slots.Count > total)
            slots.RemoveRange(total, slots.Count - total);
    }

    public InventorySlot GetSlot(int index)
    {
        if (slots == null || index < 0 || index >= slots.Count)
            return null;
        return slots[index];
    }

    // =========================
    // 아이템 분류
    // =========================
    public bool IsEquipItem(Data_table data)
    {
        if (data == null) return false;
        // 3000번대 = 장비(무기)
        return data.key >= 3000 && data.key < 4000;
    }

    public bool IsConsumableItem(Data_table data)
    {
        if (data == null) return false;
        // 2000번대 = 아이템(재료/소비)
        return data.key >= 2000 && data.key < 3000;
    }

    // =========================================================
    // ✅ AddItem: 타입에 따라 들어갈 패널 범위를 강제
    //  - 장비(무기): WeaponPanel(1~25)
    //  - 소비/재료: ItemPanel(26~50)
    //  - 0번 장비칸에는 AddItem이 절대 들어가지 않음
    // =========================================================
    public void AddItem(Data_table data, int quantity)
    {
        if (data == null || quantity <= 0)
        {
            Debug.LogWarning("[InventoryManager] AddItem(Data_table): 잘못된 인자");
            return;
        }
        AddItem(new ItemInstance(data, quantity));
    }

    public void AddItem(ItemInstance newItem)
    {
        if (newItem == null || newItem.data == null || newItem.quantity <= 0)
        {
            Debug.LogWarning("[InventoryManager] AddItem(ItemInstance): 잘못된 아이템");
            return;
        }

        // ✅ 범위 선택
        int start, end;
        if (IsEquipItem(newItem.data))
        {
            start = WeaponInvStart;
            end = WeaponInvEnd;
        }
        else
        {
            start = ItemInvStart;
            end = ItemInvEnd;
        }

        // 실패해도 기존 코드 흐름 유지 (경고만)
        bool ok = AddItemToRange(newItem, start, end);
        if (!ok)
            Debug.LogWarning($"[InventoryManager] AddItem: 범위({start}~{end})가 가득 찼습니다.");
    }

    // ✅ 범위 지정 추가(성공/실패 반환) - 장착/해제 안정성 때문에 bool로 운용
    private bool AddItemToRange(ItemInstance newItem, int start, int end)
    {
        if (newItem == null || newItem.data == null || newItem.quantity <= 0) return false;

        Data_table data = newItem.data;

        // 1) 같은 아이템 스택 채우기 (범위 내에서만)
        for (int i = start; i <= end; i++)
        {
            InventorySlot slot = slots[i];
            if (slot == null || slot.IsEmpty) continue;
            if (slot.item == null || slot.item.data == null) continue;

            if (slot.item.data.key != data.key) continue;

            int maxStack = data.MaxStack;
            if (slot.item.quantity >= maxStack) continue;

            int space = maxStack - slot.item.quantity;
            int move = Mathf.Min(space, newItem.quantity);

            slot.item.quantity += move;
            newItem.quantity -= move;

            if (newItem.quantity <= 0)
            {
                OnInventoryChanged?.Invoke();
                return true;
            }
        }

        // 2) 빈 슬롯 찾기 (범위 내에서만)
        for (int i = start; i <= end; i++)
        {
            InventorySlot slot = slots[i];
            if (slot == null) continue;

            if (slot.IsEmpty)
            {
                slot.item = new ItemInstance(data, newItem.quantity);
                newItem.quantity = 0;
                OnInventoryChanged?.Invoke();
                return true;
            }
        }

        Debug.LogWarning($"[InventoryManager] AddItemToRange FAIL: 범위({start}~{end}) 가득 참");
        return false;
    }

    // =========================
    // 드랍(버리기)
    // =========================
    public bool DropItemFromSlot(int slotIndex, int amount = 1)
    {
        Debug.Log($"[InventoryManager] DropItemFromSlot slotIndex={slotIndex}, amount={amount}");

        var slot = GetSlot(slotIndex);
        if (slot == null || slot.IsEmpty || slot.item == null || slot.item.data == null)
            return false;

        // ✅ 장비칸(0번)에서 버리기 막고 싶으면 여기서 return false 처리해도 됨
        // if (slotIndex == EquipWeaponIndex) return false;

        var itemInstance = slot.item;
        var data = itemInstance.data;

        int dropAmount = Mathf.Clamp(amount, 1, itemInstance.quantity);

        // 1) 프리팹 결정
        GameObject prefabToUse = null;
        if (dropPrefabs != null)
        {
            foreach (var entry in dropPrefabs)
            {
                if (entry == null) continue;
                if (entry.itemKey == data.key)
                {
                    prefabToUse = entry.prefab;
                    break;
                }
            }
        }
        if (prefabToUse == null) prefabToUse = defaultDropPrefab;

        // 2) 생성 위치(플레이어 기준)
        if (prefabToUse != null)
        {
            Vector3 spawnPos = Vector3.zero;
            Vector3 dir = Vector3.forward;

            if (playerInteraction != null)
            {
                Transform t = playerInteraction.transform;
                dir = t.forward;
                spawnPos = t.position + dir * 1.2f + Vector3.up * 0.3f;
            }
            else
            {
                Transform t = transform;
                dir = t.forward;
                spawnPos = t.position + dir * 2.0f + Vector3.up * 0.3f;
            }

            GameObject worldObj = Instantiate(prefabToUse, spawnPos, Quaternion.identity);
            Debug.Log($"[InventoryManager] Dropped: {worldObj.name} at {spawnPos}");

            var pickup = worldObj.GetComponent<ItemPickupFromTable>();
            if (pickup != null)
            {
                pickup.itemKey = data.key;
                pickup.quantity = dropAmount;
            }
        }

        // 3) 수량 감소
        itemInstance.quantity -= dropAmount;
        if (itemInstance.quantity <= 0)
            slot.item = null;

        OnInventoryChanged?.Invoke();
        return true;
    }

    // =========================
    // Use (소비 아이템)
    // =========================
    public bool UseItemFromSlot(int slotIndex, int amount = 1)
    {
        var slot = GetSlot(slotIndex);
        if (slot == null || slot.IsEmpty || slot.item == null || slot.item.data == null)
            return false;

        var data = slot.item.data;

        // 소비 아이템만 사용
        if (!IsConsumableItem(data))
            return false;

        int useAmount = Mathf.Clamp(amount, 1, slot.item.quantity);
        slot.item.quantity -= useAmount;
        if (slot.item.quantity <= 0) slot.item = null;

        OnInventoryChanged?.Invoke();
        return true;
    }

    // =========================================================
    // ✅ 장비 장착/해제 (단순화 버전)
    //  - EquipFromInventory(fromIndex): 장비 아이템이면 "무조건 0번"으로 이동
    //  - UnequipWeapon(): 0번 -> WeaponPanel(1~25)로 복귀
    // =========================================================
    public bool EquipFromInventory(int fromSlotIndex)
    {
        var from = GetSlot(fromSlotIndex);
        if (from == null || from.IsEmpty || from.item == null || from.item.data == null)
            return false;

        var data = from.item.data;
        if (!IsEquipItem(data))
        {
            Debug.LogWarning($"[InventoryManager] EquipFromInventory: 장비 아이템이 아님 key={data.key}");
            return false;
        }

        int equipIdx = EquipWeaponIndex; // ✅ 무조건 0번
        var equipSlot = GetSlot(equipIdx);

        // 이미 장착 중이면 해제 먼저 (실패하면 장착 중단)
        if (equipSlot != null && !equipSlot.IsEmpty && equipSlot.item != null)
        {
            if (!UnequipWeapon())
            {
                Debug.LogWarning("[InventoryManager] EquipFromInventory: 기존 무기 해제 실패(WeaponPanel 가득 참?)");
                return false;
            }
        }

        // 인벤 슬롯에서 1개만 꺼내 장착
        ItemInstance toEquip;

        if (from.item.quantity > 1)
        {
            from.item.quantity -= 1;
            toEquip = new ItemInstance(data, 1);
        }
        else
        {
            toEquip = from.item;
            from.item = null;
        }

        toEquip.equipped = true;
        if (equipSlot != null)
            equipSlot.item = toEquip;

        Debug.Log($"[InventoryManager] Equipped -> slot0 : {data.ItemName} (from={fromSlotIndex})");
        OnInventoryChanged?.Invoke();
        return true;
    }

    public bool UnequipWeapon()
    {
        int equipIdx = EquipWeaponIndex; // 0
        var equipSlot = GetSlot(equipIdx);

        if (equipSlot == null || equipSlot.IsEmpty || equipSlot.item == null)
            return false;

        ItemInstance equipped = equipSlot.item;

        // 먼저 장착칸 비우기
        equipSlot.item = null;
        equipped.equipped = false;

        // ✅ 무기는 WeaponPanel로만 되돌림 (1~25)
        bool ok = AddItemToRange(equipped, WeaponInvStart, WeaponInvEnd);
        if (!ok)
        {
            // 원복(증발 방지)
            equipped.equipped = true;
            equipSlot.item = equipped;
            Debug.LogWarning("[InventoryManager] UnequipWeapon FAIL -> reverted to slot0 (WeaponPanel full?)");
            OnInventoryChanged?.Invoke();
            return false;
        }

        Debug.Log("[InventoryManager] UnequipWeapon: 무기 해제 완료 (0 -> WeaponPanel)");
        OnInventoryChanged?.Invoke();
        return true;
    }

    /// <summary>현재 장착된 무기(0번 슬롯)를 가져옴</summary>
    public ItemInstance GetEquippedWeapon()
    {
        var equipSlot = GetSlot(EquipWeaponIndex);
        if (equipSlot == null || equipSlot.IsEmpty || equipSlot.item == null)
            return null;

        return equipSlot.item;
    }
}
