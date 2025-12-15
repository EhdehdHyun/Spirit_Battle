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

    /// <summary>
    /// 인벤토리가 바뀔 때마다 UI가 구독하는 이벤트
    /// </summary>
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

    /// <summary>
    /// 인덱스로 슬롯 가져오기
    /// </summary>
    public InventorySlot GetSlot(int index)
    {
        if (slots == null || index < 0 || index >= slots.Count)
            return null;
        return slots[index];
    }

    /// <summary>
    /// Data_table + 수량으로 바로 추가
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
    /// ItemInstance 단위로 추가 (스택 처리 포함)
    /// </summary>
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
            if (slot == null || slot.IsEmpty)
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
                Debug.Log($"[InventoryManager] AddItem: {data.ItemName} 스택에 추가 (slot {i})");
                OnInventoryChanged?.Invoke();
                return;
            }
        }

        // 2) 빈 슬롯 찾기
        for (int i = 0; i < slots.Count; i++)
        {
            InventorySlot slot = slots[i];
            if (slot == null)
                continue;

            if (slot.IsEmpty)
            {
                slot.item = new ItemInstance(data, newItem.quantity);
                newItem.quantity = 0;
                Debug.Log($"[InventoryManager] AddItem: {data.ItemName} x{slot.item.quantity} 새 슬롯 {i}에 추가");
                OnInventoryChanged?.Invoke();
                return;
            }
        }

        Debug.LogWarning("[InventoryManager] AddItem: 인벤토리가 가득 찼습니다.");
    }

    /// <summary>
    /// 인벤토리 슬롯에서 amount개를 버리고
    /// 플레이어 앞에 드랍 프리팹을 생성한다.
    /// </summary>
    public void DropItemFromSlot(int slotIndex, int amount = 1)
    {
        Debug.Log($"[InventoryManager] DropItemFromSlot 호출됨. slotIndex={slotIndex}, amount={amount}");

        var slot = GetSlot(slotIndex);
        if (slot == null || slot.IsEmpty)
        {
            Debug.Log($"[InventoryManager] DropItemFromSlot: 비어 있는 슬롯 {slotIndex}");
            return;
        }

        var itemInstance = slot.item;
        var data = itemInstance.data;
        if (data == null)
        {
            Debug.LogWarning($"[InventoryManager] DropItemFromSlot: 슬롯 {slotIndex} 의 data 가 없습니다.");
            return;
        }

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

        if (prefabToUse != null)
        {
            // 2) 플레이어 앞에 드랍
            Vector3 spawnPos;

            if (playerInteraction != null)
            {
                Transform t = playerInteraction.transform;
                spawnPos = t.position + t.forward * 1.2f + Vector3.up * 0.3f;
            }
            else
            {
                Transform t = transform;
                spawnPos = t.position + t.forward * 2f + Vector3.up * 0.3f;
                Debug.LogWarning("[InventoryManager] playerInteraction 이 설정되지 않아 InventoryManager 기준으로 드랍합니다.");
            }

            GameObject worldObj = Instantiate(prefabToUse, spawnPos, Quaternion.identity);
            Debug.Log($"[InventoryManager] 드랍 프리팹 생성: {worldObj.name} at {spawnPos}");

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