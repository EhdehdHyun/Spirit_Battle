using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class DropPrefabEntry
{
    public int itemKey;       // Data_table.key
    public GameObject prefab; // 드랍될 프리팹
}

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    [Header("전체 슬롯 크기 (Rows x Columns)")]
    public int rows = 5;
    public int columns = 10;  // 예: 5x10 = 50 슬롯

    [Tooltip("rows * columns 개의 인벤토리 슬롯")]
    public List<InventorySlot> slots = new List<InventorySlot>();

    [Header("슬롯 범위 설정")]
    [Tooltip("장비 인벤 슬롯 개수 (0 ~ equipmentSlotCount-1)")]
    public int equipmentSlotCount = 25;

    [Tooltip("아이템 인벤 슬롯 개수 (equipmentSlotCount ~ equipmentSlotCount+itemSlotCount-1)")]
    public int itemSlotCount = 25;

    [Header("참조")]
    [Tooltip("플레이어 상호작용 (드랍 위치/방향용)")]
    public PlayerInteraction playerInteraction;

    [Header("드랍 프리팹 매핑")]
    public List<DropPrefabEntry> dropPrefabs = new List<DropPrefabEntry>();
    public GameObject defaultDropPrefab;

    /// <summary>장비 슬롯 한 칸 (무기)</summary>
    public ItemInstance EquippedWeapon { get; private set; }

    /// <summary>인벤토리 내용이 바뀔 때 (UI 전체 갱신용)</summary>
    public event Action OnInventoryChanged;

    /// <summary>장비 슬롯이 바뀔 때 (장비 UI 갱신용)</summary>
    public event Action OnEquipmentChanged;

    // 내부에서만 쓸 인벤토리 타입 구분용
    private enum InventoryType
    {
        Equipment,  // 1번 인벤 (장비)
        Item        // 2번 인벤 (소비/재료)
    }

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

    /// <summary>slots 리스트를 rows*columns 크기로 맞춰준다.</summary>
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

    /// <summary>인덱스로 슬롯 가져오기 (범위 체크 포함)</summary>
    public InventorySlot GetSlot(int index)
    {
        if (slots == null || index < 0 || index >= slots.Count)
            return null;
        return slots[index];
    }

    /// <summary>Data_table + 수량으로 바로 추가</summary>
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
    /// ItemInstance 를 인벤토리에 추가.
    /// - 3000번대 아이템: 장비 인벤 (equipmentSlotCount 범위)
    /// - 2000번대/그 외: 아이템 인벤 (itemSlotCount 범위)
    /// </summary>
    public void AddItem(ItemInstance newItem)
    {
        if (newItem == null || newItem.data == null || newItem.quantity <= 0)
        {
            Debug.LogWarning("[InventoryManager] AddItem(ItemInstance): 잘못된 아이템");
            return;
        }

        var data = newItem.data;
        var invType = GetInventoryType(data);

        int totalNeeded = equipmentSlotCount + itemSlotCount;
        if (slots.Count < totalNeeded)
        {
            Debug.LogWarning($"[InventoryManager] slots.Count({slots.Count}) < equipmentSlotCount+itemSlotCount({totalNeeded})");
        }

        int equipStart = 0;
        int equipEnd = Mathf.Min(equipmentSlotCount, slots.Count);

        int itemStart = equipEnd;
        int itemEnd = Mathf.Min(equipEnd + itemSlotCount, slots.Count);

        if (invType == InventoryType.Equipment)
        {
            Debug.Log("[InventoryManager] 장비 인벤에 추가 시도");
            AddItemToRange(newItem, equipStart, equipEnd);
        }
        else
        {
            Debug.Log("[InventoryManager] 아이템 인벤에 추가 시도");
            AddItemToRange(newItem, itemStart, itemEnd);
        }

        OnInventoryChanged?.Invoke();
    }

    /// <summary>data의 key 기준으로 어느 인벤토리에 들어가는지 결정</summary>
    private InventoryType GetInventoryType(Data_table data)
    {
        if (data == null) return InventoryType.Item;

        // 예: 3000번대 => 장비, 2000번대 => 아이템
        int group = data.key / 1000;

        if (group == 3)
            return InventoryType.Equipment;
        if (group == 2)
            return InventoryType.Item;

        // 기본은 아이템 인벤
        return InventoryType.Item;
    }

    /// <summary>현재 규칙에서 "장비 아이템인가?" 판단 (Use 버튼에서 사용)</summary>
    public bool IsEquipItem(Data_table data)
    {
        return GetInventoryType(data) == InventoryType.Equipment;
    }

    /// <summary>
    /// slots 리스트의 [startIndex, endIndex) 범위 안에서만
    /// 스택 / 빈 칸을 찾아 newItem 을 채운다.
    /// </summary>
    private void AddItemToRange(ItemInstance newItem, int startIndex, int endIndex)
    {
        var data = newItem.data;

        // 1) 같은 아이템 스택 채우기
        for (int i = startIndex; i < endIndex; i++)
        {
            InventorySlot slot = slots[i];
            if (slot == null || slot.IsEmpty || slot.item == null || slot.item.data == null)
                continue;

            if (slot.item.data.key != data.key)
                continue;

            int maxStack = data.MaxStack;
            if (slot.item.quantity >= maxStack)
                continue;

            int space = maxStack - slot.item.quantity;
            int move = Mathf.Min(space, newItem.quantity);

            slot.item.quantity += move;
            newItem.quantity -= move;

            if (newItem.quantity <= 0)
            {
                Debug.Log($"[InventoryManager] AddItemToRange: {data.ItemName} 스택 추가 완료 (slot {i})");
                return;
            }
        }

        // 2) 빈 슬롯 찾기
        for (int i = startIndex; i < endIndex; i++)
        {
            InventorySlot slot = slots[i];
            if (slot == null)
                continue;

            if (slot.IsEmpty || slot.item == null || slot.item.data == null)
            {
                slot.item = new ItemInstance(data, newItem.quantity);
                Debug.Log($"[InventoryManager] AddItemToRange: {data.ItemName} x{slot.item.quantity} 새 슬롯 {i}에 추가");
                newItem.quantity = 0;
                return;
            }
        }

        Debug.LogWarning($"[InventoryManager] AddItemToRange: 범위 {startIndex}~{endIndex} 인벤이 가득 참");
    }

    // ─────────────────────────────────────
    //   장비 관련
    // ─────────────────────────────────────

    /// <summary>
    /// 인벤토리의 slotIndex 에 있는 아이템 1개를 장비 슬롯(무기)에 장착
    /// </summary>
    public void EquipFromInventory(int slotIndex)
    {
        var slot = GetSlot(slotIndex);
        if (slot == null || slot.IsEmpty || slot.item == null || slot.item.data == null)
        {
            Debug.LogWarning($"[InventoryManager] EquipFromInventory: 잘못된 슬롯 {slotIndex}");
            return;
        }

        var item = slot.item;
        var data = item.data;

        if (!IsEquipItem(data))
        {
            Debug.Log($"[InventoryManager] {data.ItemName} 은(는) 장비 아이템이 아니라서 장착 불가");
            return;
        }

        // 기존 장비가 있으면 인벤토리로 되돌리기
        if (EquippedWeapon != null && EquippedWeapon.data != null)
        {
            Debug.Log($"[InventoryManager] 기존 장비 {EquippedWeapon.data.ItemName} 인벤토리로 되돌림");
            AddItem(new ItemInstance(EquippedWeapon.data, 1));
        }

        // 인벤토리에서 1개 감소
        item.quantity -= 1;
        if (item.quantity <= 0)
            slot.item = null;

        // 장비 슬롯에 세팅
        EquippedWeapon = new ItemInstance(data, 1);
        EquippedWeapon.equipped = true;

        Debug.Log($"[InventoryManager] {data.ItemName} 장착 완료 (slot {slotIndex})");

        OnInventoryChanged?.Invoke();
        OnEquipmentChanged?.Invoke();
    }

    /// <summary>현재 장착된 무기를 해제하고 인벤토리로 되돌린다</summary>
    public void UnequipWeaponToInventory()
    {
        if (EquippedWeapon == null || EquippedWeapon.data == null)
            return;

        var data = EquippedWeapon.data;
        Debug.Log($"[InventoryManager] {data.ItemName} 장비 해제 → 인벤토리로 이동");

        AddItem(new ItemInstance(data, 1));

        // 장비 슬롯 비우기
        EquippedWeapon = null;

        // UI 갱신 이벤트
        OnInventoryChanged?.Invoke();
        OnEquipmentChanged?.Invoke();
    }



    /// <summary>
    /// 인벤토리 슬롯에서 amount개를 버리고
    /// 플레이어가 바라보는 방향 앞에 드랍 프리팹을 생성한다.
    /// </summary>
    public void DropItemFromSlot(int slotIndex, int amount = 1)
    {
        Debug.Log($"[InventoryManager] DropItemFromSlot 호출됨. slotIndex={slotIndex}, amount={amount}");

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
                if (entry == null || entry.prefab == null) continue;
                if (entry.itemKey == data.key)
                {
                    prefabToUse = entry.prefab;
                    break;
                }
            }
        }

        if (prefabToUse == null)
            prefabToUse = defaultDropPrefab;

        if (prefabToUse != null)
        {
            // 2) 플레이어 앞에 드랍 위치 계산
            Transform baseTransform = transform;

            if (playerInteraction != null)
                baseTransform = playerInteraction.transform;

            Vector3 dropDir;
            if (playerInteraction != null && playerInteraction.playerCamera != null)
            {
                dropDir = playerInteraction.playerCamera.transform.forward;
            }
            else
            {
                dropDir = baseTransform.forward;
            }

            dropDir.y = 0f;
            if (dropDir.sqrMagnitude < 0.0001f)
                dropDir = Vector3.forward;
            dropDir.Normalize();

            Vector3 spawnPos = baseTransform.position + dropDir * 1.5f + Vector3.up * 0.3f;

            Debug.Log($"[InventoryManager] Drop dir = {dropDir}, spawnPos = {spawnPos}");

            GameObject worldObj = Instantiate(prefabToUse, spawnPos, Quaternion.identity);
            Debug.Log($"[InventoryManager] 드랍 프리팹 생성: {worldObj.name}");

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

        // 3) 인벤에서 수량 감소
        itemInstance.quantity -= dropAmount;
        if (itemInstance.quantity <= 0)
        {
            slot.item = null;
        }

        OnInventoryChanged?.Invoke();
        Debug.Log($"[InventoryManager] DropItemFromSlot: {data.ItemName} x{dropAmount} 버림 (slot {slotIndex})");
    }
}
