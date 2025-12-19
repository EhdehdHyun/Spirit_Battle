using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class DropPrefabEntry
{
    [Tooltip("이 프리팹과 연결할 아이템 key (Data_table.key)")]
    public int itemKey;

    [Tooltip("월드에 떨어뜨릴 프리팹")]
    public GameObject prefab;
}

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    [Header("슬롯 개수")]
    [Tooltip("1번 인벤(장비) 슬롯 개수")]
    public int equipSlotCount = 25;

    [Tooltip("2번 인벤(재료) 슬롯 개수")]
    public int materialSlotCount = 25;

    [Tooltip("전체 슬롯 리스트 (장비 + 재료)")]
    public List<InventorySlot> slots = new List<InventorySlot>();

    [Header("참조")]
    [Tooltip("플레이어 상호작용 스크립트 (플레이어 위치/방향 / 카메라 사용)")]
    public PlayerInteraction playerInteraction;

    [Header("드랍 프리팹 매핑")]
    [Tooltip("itemKey(아이템 코드) → 프리팹 매핑 리스트")]
    public List<DropPrefabEntry> dropPrefabs = new List<DropPrefabEntry>();

    [Tooltip("매핑을 찾지 못했을 때 사용할 기본 프리팹 (없으면 null 가능)")]
    public GameObject defaultDropPrefab;

    /// <summary>
    /// 인벤토리 데이터가 바뀔 때마다 UI가 구독하는 이벤트
    /// </summary>
    public event Action OnInventoryChanged;

    // ───────────────── 인덱스 구간 헬퍼 ─────────────────

    /// <summary>장비 인벤 시작 인덱스 (항상 0)</summary>
    public int EquipStartIndex => 0;

    /// <summary>장비 인벤 끝 인덱스 (미포함)</summary>
    public int EquipEndIndex => equipSlotCount;

    /// <summary>재료 인벤 시작 인덱스</summary>
    public int MaterialStartIndex => equipSlotCount;

    /// <summary>재료 인벤 끝 인덱스 (미포함)</summary>
    public int MaterialEndIndex => equipSlotCount + materialSlotCount;

    // ───────────────── Unity 라이프사이클 ─────────────────

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
        Debug.Log($"[InventoryManager] Awake: total slots = {slots.Count}");
    }

    /// <summary>
    /// 전체 슬롯 리스트를 equipSlotCount + materialSlotCount 만큼 초기화
    /// </summary>
    private void InitSlots()
    {
        int total = equipSlotCount + materialSlotCount;

        if (slots == null)
            slots = new List<InventorySlot>(total);

        // 부족하면 새 슬롯 추가
        while (slots.Count < total)
            slots.Add(new InventorySlot());

        // 너무 많으면 잘라내기
        if (slots.Count > total)
            slots.RemoveRange(total, slots.Count - total);
    }

    // ───────────────── 기본 유틸 ─────────────────

    /// <summary>
    /// 인덱스로 슬롯 가져오기 (범위 밖이면 null)
    /// </summary>
    public InventorySlot GetSlot(int index)
    {
        if (slots == null || index < 0 || index >= slots.Count)
            return null;

        return slots[index];
    }

    // ───────────────── 아이템 추가 ─────────────────

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

        // ── 1) 어떤 인벤 구역(장비/재료)을 사용할지 결정 ──
        int startIndex;
        int endIndex;

        // 3000번대 → 장비 인벤
        if (data.key >= 3000 && data.key < 4000)
        {
            startIndex = EquipStartIndex;
            endIndex = EquipEndIndex;
        }
        // 2000번대 → 재료 인벤
        else if (data.key >= 2000 && data.key < 3000)
        {
            startIndex = MaterialStartIndex;
            endIndex = MaterialEndIndex;
        }
        else
        {
            // 그 외는 일단 재료 인벤으로 보냄 (원하면 규칙 수정 가능)
            startIndex = MaterialStartIndex;
            endIndex = MaterialEndIndex;
        }

        // ── 2) 먼저 같은 아이템 스택을 채운다 ──
        for (int i = startIndex; i < endIndex; i++)
        {
            var slot = slots[i];
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

        // ── 3) 남은 수량이 있으면 빈 슬롯을 찾는다 ──
        for (int i = startIndex; i < endIndex; i++)
        {
            var slot = slots[i];
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

        Debug.LogWarning($"[InventoryManager] AddItem: 인벤토리 구역이 가득 찼습니다. ({startIndex} ~ {endIndex - 1})");
    }

    // ───────────────── 아이템 드랍 ─────────────────

    /// <summary>
    /// 인벤토리 슬롯에서 amount 개를 버리고,
    /// 플레이어가 바라보는 방향 앞에 프리팹을 생성한다.
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
            Debug.LogWarning($"[InventoryManager] DropItemFromSlot: 슬롯 {slotIndex} 의 Data_table 이 없습니다.");
            return;
        }

        int dropAmount = Mathf.Clamp(amount, 1, itemInstance.quantity);

        // ── 1) 어떤 프리팹을 쓸지 결정 (itemKey → prefab 매핑) ──
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

        // ── 2) 월드에 프리팹 생성 ──
        // 2) 월드에 프리팹 생성
        if (prefabToUse != null)
        {
            Vector3 spawnPos;
            Quaternion spawnRot = Quaternion.identity;

            // 1) 기준 위치는 "플레이어 위치"
            Transform playerTr = playerInteraction != null ? playerInteraction.transform : transform;

            // 2) 방향은 카메라가 보는 방향 (없으면 플레이어 forward)
            Vector3 forward;

            if (playerInteraction != null && playerInteraction.playerCamera != null)
            {
                forward = playerInteraction.playerCamera.transform.forward;
            }
            else
            {
                forward = playerTr.forward;
            }

            // 3) 수평 방향만 사용 (y = 0), 너무 작으면 플레이어 forward 사용
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.0001f)
                forward = playerTr.forward;

            forward.Normalize();

            // 4) 플레이어 앞 dropDistance 만큼, 조금 위쪽에 떨어뜨리기
            float dropDistance = 1.5f;
            float dropHeight = 0.3f;

            spawnPos = playerTr.position + forward * dropDistance + Vector3.up * dropHeight;

            // 디버그용 Ray(씬 뷰에서 노란 선으로 보임)
            Debug.DrawRay(playerTr.position + Vector3.up * 0.5f, forward * dropDistance, Color.yellow, 2f);
            Debug.Log($"[InventoryManager] Drop dir = {forward}, spawnPos = {spawnPos}");

            GameObject worldObj = Instantiate(prefabToUse, spawnPos, spawnRot);
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


        // ── 3) 인벤에서 수량 감소 ──
        itemInstance.quantity -= dropAmount;
        if (itemInstance.quantity <= 0)
        {
            slot.item = null;
        }

        OnInventoryChanged?.Invoke();
        Debug.Log($"[InventoryManager] DropItemFromSlot: {data.ItemName} x{dropAmount} 버림 (slot {slotIndex})");
    }
}
