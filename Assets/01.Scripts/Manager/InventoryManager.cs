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

    [Header("Grid Size (Rows x Columns)")]
    public int rows = 5;
    public int columns = 5;

    [Tooltip("인벤토리 슬롯 리스트 (rows * columns 개)")]
    public List<InventorySlot> slots = new List<InventorySlot>();

    [Header("참조")]
    [Tooltip("플레이어 상호작용 스크립트 (플레이어 위치/방향 얻기용)")]
    public PlayerInteraction playerInteraction;

    [Header("드랍 프리팹 매핑")]
    public List<DropPrefabEntry> dropPrefabs = new List<DropPrefabEntry>();
    public GameObject defaultDropPrefab;

    // ===== 장착 슬롯 인덱스(기준) =====
    // 어제 기준: 무기 장착칸은 0번을 사용한다고 했던 흐름을 유지
    // (너희 프로젝트에서 장착칸을 별도 인덱스로 쓰고 있다면 이 값만 바꾸면 됨)
    public int WeaponEquipStartIndex = 0;

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

        InitSlots();
        Debug.Log("[InventoryManager] Awake: slot count = " + slots.Count);
    }

    private void InitSlots()
    {
        int total = rows * columns;

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
        // 3000번대 = 장비
        return data.key >= 3000 && data.key < 4000;
    }

    public bool IsConsumableItem(Data_table data)
    {
        if (data == null) return false;
        // 2000번대 = 아이템(재료/소비)
        return data.key >= 2000 && data.key < 3000;
    }

    // =========================
    // AddItem
    // =========================
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

        Data_table data = newItem.data;

        // 1) 같은 아이템 스택 채우기
        for (int i = 0; i < slots.Count; i++)
        {
            InventorySlot slot = slots[i];
            if (slot == null || slot.IsEmpty) continue;

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
                return;
            }
        }

        // 2) 빈 슬롯 찾기
        for (int i = 0; i < slots.Count; i++)
        {
            InventorySlot slot = slots[i];
            if (slot == null) continue;

            if (slot.IsEmpty)
            {
                slot.item = new ItemInstance(data, newItem.quantity);
                newItem.quantity = 0;
                OnInventoryChanged?.Invoke();
                return;
            }
        }

        Debug.LogWarning("[InventoryManager] AddItem: 인벤토리가 가득 찼습니다.");
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
                // playerInteraction에 public getter가 없다면, transform 기준으로라도 떨어지게
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
    // Use (소비 아이템) - 지금은 자리만
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

        // TODO: apple 같은 회복 로직은 여기서 구현 예정
        // 지금은 "사용 성공" 흐름만 만들고, 수량만 감소시키거나 안 건드릴지 선택
        int useAmount = Mathf.Clamp(amount, 1, slot.item.quantity);
        slot.item.quantity -= useAmount;
        if (slot.item.quantity <= 0) slot.item = null;

        OnInventoryChanged?.Invoke();
        return true;
    }

    // =========================
    // 장비 장착/해제
    // =========================
    public bool EquipFromInventory(int slotIndex)
    {
        var from = GetSlot(slotIndex);
        if (from == null || from.IsEmpty || from.item == null || from.item.data == null)
            return false;

        var data = from.item.data;
        if (!IsEquipItem(data))
        {
            Debug.LogWarning($"[InventoryManager] EquipFromInventory: 장비 아이템이 아님 key={data.key}");
            return false;
        }

        int equipIdx = WeaponEquipStartIndex; // 무기 장착칸
        var equipSlot = GetSlot(equipIdx);

        // 이미 장착 중이면 먼저 해제
        if (equipSlot != null && !equipSlot.IsEmpty && equipSlot.item != null)
        {
            UnequipWeapon();
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

        Debug.Log($"[InventoryManager] EquipFromInventory: {data.ItemName} 장착 (equipSlot={equipIdx})");
        OnInventoryChanged?.Invoke();
        return true;
    }

    public bool UnequipWeapon()
    {
        int equipIdx = WeaponEquipStartIndex;
        var equipSlot = GetSlot(equipIdx);

        if (equipSlot == null || equipSlot.IsEmpty || equipSlot.item == null)
            return false;

        ItemInstance equipped = equipSlot.item;
        equipped.equipped = false;
        equipSlot.item = null;

        // 다시 인벤으로 넣기
        AddItem(equipped);

        Debug.Log("[InventoryManager] UnequipWeapon: 무기 해제 완료");
        OnInventoryChanged?.Invoke();
        return true;
    }

    /// <summary>
    /// PlayerWeaponVisual 같은 곳에서 현재 장착된 무기를 읽어오기 위한 함수
    /// </summary>
    public ItemInstance GetEquippedWeapon()
    {
        int equipIdx = WeaponEquipStartIndex;
        var equipSlot = GetSlot(equipIdx);

        if (equipSlot == null || equipSlot.IsEmpty || equipSlot.item == null)
            return null;

        if (equipSlot.item.equipped == false)
            return null;

        return equipSlot.item;
    }
}
