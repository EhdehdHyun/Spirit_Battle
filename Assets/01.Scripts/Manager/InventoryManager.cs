using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class DropPrefabEntry
{
    public int itemKey;      // Data_table.key
    public GameObject prefab;
}

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    [Header("인벤토리 그리드 (오른쪽 패널)")]
    [SerializeField] private int rows = 5;
    [SerializeField] private int columns = 5;

    // 0번은 무기 장비칸, 1 ~ (1 + rows*columns - 1) 은 일반 인벤 칸
    [Header("슬롯 인덱스 설정")]
    [SerializeField] private int weaponEquipSlotIndex = 0;   // 무기 장비 슬롯 (왼쪽 큰 칸)
    [SerializeField] private int itemSlotStartIndex = 1;     // 일반 인벤 시작 인덱스 (1 고정)

    public int ItemSlotCount => rows * columns;              // 일반 인벤 칸 수
    public int ItemSlotEndExclusive => itemSlotStartIndex + ItemSlotCount;

    [Header("슬롯 데이터")]
    public List<InventorySlot> slots = new List<InventorySlot>();

    [Header("드랍 관련")]
    [SerializeField] private PlayerInteraction playerInteraction;
    [SerializeField] private List<DropPrefabEntry> dropPrefabs = new List<DropPrefabEntry>();
    [SerializeField] private GameObject defaultDropPrefab;

    /// <summary>
    /// 인벤토리 데이터가 바뀔 때마다 호출되는 이벤트 (UI가 구독)
    /// </summary>
    public event Action OnInventoryChanged;

    #region Singleton & 초기화

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
        Debug.Log($"[InventoryManager] Awake: slotCount={slots.Count} (equip=0, items={ItemSlotCount})");
    }

    private void InitSlots()
    {
        // 0 : 장비칸, 1~ : 인벤 칸
        int total = itemSlotStartIndex + ItemSlotCount; // 예: 1 + 25 = 26

        if (slots == null)
            slots = new List<InventorySlot>(total);

        while (slots.Count < total)
            slots.Add(new InventorySlot());

        if (slots.Count > total)
            slots.RemoveRange(total, slots.Count - total);
    }

    #endregion

    #region Slot 접근

    public InventorySlot GetSlot(int index)
    {
        if (slots == null || index < 0 || index >= slots.Count)
            return null;

        return slots[index];
    }

    public int GetWeaponEquipSlotIndex() => weaponEquipSlotIndex;
    public int GetItemSlotStartIndex() => itemSlotStartIndex;
    public int GetItemSlotEndExclusive() => ItemSlotEndExclusive;

    #endregion

    #region 아이템 추가

    /// <summary>
    /// Data_table 과 수량으로 바로 추가하는 편의 함수
    /// </summary>
    public void AddItem(Data_table data, int quantity)
    {
        if (data == null || quantity <= 0)
        {
            Debug.LogWarning("[InventoryManager] AddItem(Data_table): 잘못된 인자");
            return;
        }

        AddItem(new ItemInstance(data, quantity));
    }

    /// <summary>
    /// ItemInstance 단위로 인벤토리에 추가
    /// (스택 처리, 빈칸 탐색 포함 / 1~N 범위만 사용)
    /// </summary>
    public void AddItem(ItemInstance newItem)
    {
        if (newItem == null || newItem.data == null || newItem.quantity <= 0)
        {
            Debug.LogWarning("[InventoryManager] AddItem(ItemInstance): 잘못된 아이템");
            return;
        }

        Data_table data = newItem.data;
        int originalQty = newItem.quantity;

        int slotIndex = AddItemToItemRange(newItem);

        if (slotIndex < 0)
        {
            Debug.LogWarning("[InventoryManager] AddItem: 인벤토리가 가득 찼습니다.");
            return;
        }

        Debug.Log($"[InventoryManager] AddItem: {data.ItemName} x{originalQty} → slot {slotIndex}");
        OnInventoryChanged?.Invoke();
    }

    /// <summary>
    /// 일반 인벤 구간 [itemSlotStartIndex ~ ItemSlotEndExclusive) 에
    /// ItemInstance 를 추가하고, 실제 들어간 인덱스를 반환. 실패 시 -1.
    /// newItem.quantity 는 남은 수량 (성공 시 0) 으로 갱신됨.
    /// </summary>
    private int AddItemToItemRange(ItemInstance newItem)
    {
        int start = itemSlotStartIndex;
        int end = ItemSlotEndExclusive;

        // 1) 같은 아이템 스택 먼저 채우기
        for (int i = start; i < end; i++)
        {
            var slot = GetSlot(i);
            if (slot == null || slot.IsEmpty || slot.item == null || slot.item.data == null)
                continue;

            if (slot.item.data.key != newItem.data.key)
                continue;

            int maxStack = newItem.data.MaxStack;
            if (slot.item.quantity >= maxStack)
                continue;

            int space = maxStack - slot.item.quantity;
            int move = Mathf.Min(space, newItem.quantity);

            slot.item.quantity += move;
            newItem.quantity -= move;

            if (newItem.quantity <= 0)
                return i;
        }

        // 2) 비어 있는 슬롯 찾기
        for (int i = start; i < end; i++)
        {
            var slot = GetSlot(i);
            if (slot == null) continue;

            if (slot.IsEmpty)
            {
                slot.item = new ItemInstance(newItem.data, newItem.quantity);
                newItem.quantity = 0;
                return i;
            }
        }

        // 자리가 없음
        return -1;
    }

    #endregion

    #region 장비 / 해제 / 조회

    /// <summary>
    /// 이 Data_table 이 "장비 아이템" 인지 판별
    /// (무기 타입 + 3000번대 키)
    /// </summary>
    public bool IsEquipItem(Data_table data)
    {
        if (data == null) return false;

        if (data.ItemType == DesignEnums.ItemTypes.Weapon)
            return true;

        // 3000번대는 전부 장비 취급
        if (data.key >= 3000 && data.key < 4000)
            return true;

        return false;
    }

    /// <summary>
    /// 일반 인벤 슬롯(fromIndex)의 아이템을 무기 장비 슬롯(0번) 으로 이동
    /// </summary>
    public void EquipFromInventory(int fromIndex)
    {
        var fromSlot = GetSlot(fromIndex);
        if (fromSlot == null || fromSlot.IsEmpty || fromSlot.item == null || fromSlot.item.data == null)
        {
            Debug.LogWarning($"[InventoryManager] EquipFromInventory: 잘못된 슬롯 index={fromIndex}");
            return;
        }

        var item = fromSlot.item;
        var data = item.data;

        if (!IsEquipItem(data))
        {
            Debug.Log($"[InventoryManager] EquipFromInventory: 장비 아이템이 아님 ({data.ItemName})");
            return;
        }

        int equipIndex = weaponEquipSlotIndex;
        var equipSlot = GetSlot(equipIndex);
        if (equipSlot == null)
        {
            Debug.LogWarning("[InventoryManager] EquipFromInventory: 장비 슬롯을 찾을 수 없습니다.");
            return;
        }

        // 1) 기존에 장착된 무기가 있으면 → 일반 인벤 구간으로 되돌리기
        if (!equipSlot.IsEmpty && equipSlot.item != null)
        {
            int movedIndex = AddItemToItemRange(equipSlot.item);
            if (movedIndex < 0)
            {
                Debug.LogWarning("[InventoryManager] EquipFromInventory: 인벤이 가득 차서 기존 무기를 되돌릴 수 없습니다.");
                return;
            }
            equipSlot.item = null;
        }

        // 2) 선택한 슬롯 아이템을 장비 슬롯으로 이동
        equipSlot.item = item;
        fromSlot.item = null;

        Debug.Log($"[InventoryManager] {data.ItemName} 장착 완료 (from {fromIndex} → equip {equipIndex})");

        OnInventoryChanged?.Invoke();
    }

    /// <summary>
    /// 무기 장비 슬롯(0번)에서 해제 → 일반 인벤으로 되돌리기
    /// </summary>
    public void UnequipWeapon()
    {
        int equipIndex = weaponEquipSlotIndex;
        var equipSlot = GetSlot(equipIndex);
        if (equipSlot == null || equipSlot.IsEmpty || equipSlot.item == null)
            return;

        var item = equipSlot.item;

        int movedIndex = AddItemToItemRange(item);
        if (movedIndex < 0)
        {
            Debug.LogWarning("[InventoryManager] UnequipWeapon: 인벤이 가득 차서 장비 해제가 안됩니다.");
            return;
        }

        equipSlot.item = null;

        Debug.Log($"[InventoryManager] 무기 해제 완료 (equip {equipIndex} → slot {movedIndex})");

        OnInventoryChanged?.Invoke();
    }

    /// <summary>
    /// 현재 장착된 무기 ItemInstance 반환 (없으면 null)
    /// </summary>
    public ItemInstance GetEquippedWeapon()
    {
        int equipIndex = weaponEquipSlotIndex;
        var slot = GetSlot(equipIndex);
        if (slot != null && !slot.IsEmpty && slot.item != null && slot.item.data != null && IsEquipItem(slot.item.data))
        {
            return slot.item;
        }

        Debug.Log("[GetEquippedWeapon] 장착된 무기를 찾지 못했습니다.");
        return null;
    }

    #endregion

    #region 아이템 드랍

    /// <summary>
    /// 인벤토리 슬롯에서 amount 개를 버리고, 플레이어 앞에 프리팹을 생성
    /// </summary>
    public void DropItemFromSlot(int slotIndex, int amount = 1)
    {
        var slot = GetSlot(slotIndex);
        if (slot == null || slot.IsEmpty || slot.item == null || slot.item.data == null)
        {
            Debug.Log($"[InventoryManager] DropItemFromSlot: 비어 있는 슬롯 {slotIndex}");
            return;
        }

        var itemInstance = slot.item;
        var data = itemInstance.data;

        int dropAmount = Mathf.Clamp(amount, 1, itemInstance.quantity);

        // 1) 어떤 프리팹을 쓸지 결정 (itemKey → prefab 매핑)
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

        if (prefabToUse == null)
            prefabToUse = defaultDropPrefab;

        // 2) 실제 월드에 생성
        if (prefabToUse != null)
        {
            Vector3 spawnPos;
            Vector3 dropDir = Vector3.forward;

            if (playerInteraction != null && playerInteraction.playerCamera != null)
            {
                var cam = playerInteraction.playerCamera;
                dropDir = cam.transform.forward;
                dropDir.y = 0f;

                if (dropDir.sqrMagnitude < 0.0001f)
                    dropDir = playerInteraction.transform.forward;

                dropDir = dropDir.normalized;

                spawnPos = playerInteraction.transform.position + dropDir * 1.5f + Vector3.up * 0.3f;
            }
            else
            {
                // 혹시 참조가 안 되어 있으면 매니저 위치 기준
                spawnPos = transform.position + Vector3.forward * 2f + Vector3.up * 0.3f;
            }

            Debug.Log($"[InventoryManager] Drop dir = {dropDir}, spawnPos = {spawnPos}");

            GameObject worldObj = Instantiate(prefabToUse, spawnPos, Quaternion.identity);

            var pickup = worldObj.GetComponent<ItemPickupFromTable>();
            if (pickup != null)
            {
                pickup.itemKey = data.key;
                pickup.quantity = dropAmount;
            }
        }
        else
        {
            Debug.LogWarning("[InventoryManager] DropItemFromSlot: 사용할 드랍 프리팹이 없습니다.");
        }

        // 3) 인벤에서 수량 감소 / 비우기
        itemInstance.quantity -= dropAmount;
        if (itemInstance.quantity <= 0)
        {
            slot.item = null;
        }

        OnInventoryChanged?.Invoke();
        Debug.Log($"[InventoryManager] DropItemFromSlot: {data.ItemName} x{dropAmount} 버림 (slot {slotIndex})");
    }

    #endregion
}
