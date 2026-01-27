using System;
using System.Collections.Generic;
using System.Reflection;
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

    public const int EquipWeaponIndex = 0;

    public const int WeaponInvStart = 1;
    public const int WeaponInvCount = 25;
    public const int WeaponInvEnd = WeaponInvStart + WeaponInvCount - 1;

    public const int ItemInvStart = 26;
    public const int ItemInvCount = 25;
    public const int ItemInvEnd = ItemInvStart + ItemInvCount - 1;

    public const int TotalSlotCount = 51;

    [Header("UI 참조 (필수 연결)")]
    public GameObject inventoryUI;

    [Header("Input Controller (필수 연결)")]
    public PlayerInputController playerInput;

    [Header("Grid Size (기존 변수 유지용)")]
    public int rows = 5;
    public int columns = 5;

    public List<InventorySlot> slots = new List<InventorySlot>();

    [Header("참조")]
    public PlayerInteraction playerInteraction;

    [Header("드랍 프리팹 매핑")]
    public List<DropPrefabEntry> dropPrefabs = new List<DropPrefabEntry>();
    public GameObject defaultDropPrefab;

    [Header("장착 슬롯 인덱스(고정)")]
    public int WeaponEquipStartIndex = EquipWeaponIndex;

    public event Action OnInventoryChanged;

    [Serializable]
    public class HealConsumableEntry
    {
        public int itemKey;
        public int healAmount;
    }

    [SerializeField] private List<HealConsumableEntry> healConsumables = new List<HealConsumableEntry>();
    [SerializeField] private string playerTag = "Player";

    private GameObject _playerObj;
    private object _healTarget;
    private MethodInfo _healMethod;
    private bool _healParamIsInt;

    private bool isOpen = false;

    private static readonly string[] HealMethodCandidates =
    {
        "TryHeal", "Heal", "AddHp", "RecoverHp", "RestoreHp", "RestoreHP"
    };

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(Instance.gameObject);
        }

        Instance = this;
        WeaponEquipStartIndex = EquipWeaponIndex;

        InitSlots();
        BindPlayerAndHealMethod();
        if (GameManager.Instance != null)
        {
            GameManager.Instance.inventoryManager = this;
        }
    }

    private void Start()
    {
        if (inventoryUI != null)
        {
            inventoryUI.SetActive(false);
        }

        isOpen = false;

        if (CursorManager.Instance != null)
        {
            CursorManager.Instance.SetUIActive(false);
        }
    }

    public void ToggleInventory()
    {
        isOpen = !isOpen;

        if (inventoryUI != null)
        {
            inventoryUI.SetActive(isOpen);
        }

        if (CursorManager.Instance != null)
        {
            CursorManager.Instance.SetUIActive(isOpen);
        }

        if (isOpen)
        {
            Time.timeScale = 0f;
            if (playerInput != null) playerInput.Lock();
        }
        else
        {
            Time.timeScale = 1f;
            if (playerInput != null)
            {
                playerInput.ResetInputState();
                playerInput.Unlock();
            }
        }
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

    public bool IsEquipItem(Data_table data)
    {
        if (data == null) return false;
        return data.key >= 3000 && data.key < 4000;
    }

    public bool IsConsumableItem(Data_table data)
    {
        if (data == null) return false;
        return data.key >= 2000 && data.key < 3000;
    }

    public void AddItem(Data_table data, int quantity)
    {
        if (data == null || quantity <= 0) return;
        AddItem(new ItemInstance(data, quantity));
    }

    public void AddItem(ItemInstance newItem)
    {
        if (newItem == null || newItem.data == null || newItem.quantity <= 0) return;

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

        AddItemToRange(newItem, start, end);
    }

    private bool AddItemToRange(ItemInstance newItem, int start, int end)
    {
        if (newItem == null || newItem.data == null || newItem.quantity <= 0) return false;

        Data_table data = newItem.data;

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
        return false;
    }

    public bool DropItemFromSlot(int slotIndex, int amount = 1)
    {
        var slot = GetSlot(slotIndex);
        if (slot == null || slot.IsEmpty || slot.item == null || slot.item.data == null)
            return false;

        var itemInstance = slot.item;
        var data = itemInstance.data;

        int dropAmount = Mathf.Clamp(amount, 1, itemInstance.quantity);

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

            var pickup = worldObj.GetComponent<ItemPickupFromTable>();
            if (pickup != null)
            {
                pickup.itemKey = data.key;
                pickup.quantity = dropAmount;
            }
        }

        itemInstance.quantity -= dropAmount;
        if (itemInstance.quantity <= 0)
            slot.item = null;

        OnInventoryChanged?.Invoke();
        return true;
    }

    public bool UseItemFromSlot(int slotIndex, int amount = 1)
    {
        var slot = GetSlot(slotIndex);
        if (slot == null || slot.IsEmpty || slot.item == null || slot.item.data == null)
            return false;

        var data = slot.item.data;

        if (!IsConsumableItem(data))
            return false;

        int useAmount = Mathf.Clamp(amount, 1, slot.item.quantity);

        bool applied = TryApplyConsumableEffect(data, useAmount);
        if (!applied) return false;

        slot.item.quantity -= useAmount;
        if (slot.item.quantity <= 0) slot.item = null;

        OnInventoryChanged?.Invoke();
        return true;
    }

    private bool TryApplyConsumableEffect(Data_table data, int amount)
    {
        if (data == null || amount <= 0) return false;

        int healPerOne = 0;
        for (int i = 0; i < healConsumables.Count; i++)
        {
            if (healConsumables[i].itemKey == data.key)
            {
                healPerOne = healConsumables[i].healAmount;
                break;
            }
        }

        if (healPerOne <= 0) return false;

        int totalHeal = healPerOne * amount;

        if (_playerObj == null || _healMethod == null || _healTarget == null)
            BindPlayerAndHealMethod();

        if (_playerObj == null || _healMethod == null || _healTarget == null) return false;

        object ret = _healParamIsInt
            ? _healMethod.Invoke(_healTarget, new object[] { totalHeal })
            : _healMethod.Invoke(_healTarget, new object[] { (float)totalHeal });

        if (ret is bool b) return b;

        return true;
    }

    private void BindPlayerAndHealMethod()
    {
        _playerObj = null;
        _healTarget = null;
        _healMethod = null;

        var p = GameObject.FindGameObjectWithTag(playerTag);
        if (p == null) return;

        _playerObj = p;

        var comps = p.GetComponents<MonoBehaviour>();
        foreach (var c in comps)
        {
            if (c == null) continue;
            var t = c.GetType();

            for (int i = 0; i < HealMethodCandidates.Length; i++)
            {
                string name = HealMethodCandidates[i];

                var mInt = t.GetMethod(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                    null, new[] { typeof(int) }, null);
                if (mInt != null)
                {
                    _healTarget = c;
                    _healMethod = mInt;
                    _healParamIsInt = true;
                    return;
                }

                var mFloat = t.GetMethod(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                    null, new[] { typeof(float) }, null);
                if (mFloat != null)
                {
                    _healTarget = c;
                    _healMethod = mFloat;
                    _healParamIsInt = false;
                    return;
                }
            }
        }
    }

    public bool EquipFromInventory(int fromSlotIndex)
    {
        var from = GetSlot(fromSlotIndex);
        if (from == null || from.IsEmpty || from.item == null || from.item.data == null)
            return false;

        var data = from.item.data;
        if (!IsEquipItem(data)) return false;

        int equipIdx = EquipWeaponIndex;
        var equipSlot = GetSlot(equipIdx);

        if (equipSlot != null && !equipSlot.IsEmpty && equipSlot.item != null)
        {
            if (!UnequipWeapon()) return false;
        }

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

        OnInventoryChanged?.Invoke();
        return true;
    }

    public bool UnequipWeapon()
    {
        int equipIdx = EquipWeaponIndex;
        var equipSlot = GetSlot(equipIdx);

        if (equipSlot == null || equipSlot.IsEmpty || equipSlot.item == null)
            return false;

        ItemInstance equipped = equipSlot.item;
        equipSlot.item = null;
        equipped.equipped = false;

        bool ok = AddItemToRange(equipped, WeaponInvStart, WeaponInvEnd);
        if (!ok)
        {
            equipped.equipped = true;
            equipSlot.item = equipped;
            OnInventoryChanged?.Invoke();
            return false;
        }
        OnInventoryChanged?.Invoke();
        return true;
    }

    public ItemInstance GetEquippedWeapon()
    {
        var equipSlot = GetSlot(EquipWeaponIndex);
        if (equipSlot == null || equipSlot.IsEmpty || equipSlot.item == null)
            return null;

        return equipSlot.item;
    }

    public void ClearSlot(int slotIndex)
    {
        var slot = GetSlot(slotIndex);
        if (slot == null || slot.IsEmpty || slot.item == null) return;
        slot.item = null;
        OnInventoryChanged?.Invoke();
    }

    public bool HasItem(int itemKey, int amount)
    {
        if (amount <= 0) return true;

        int total = 0;

        for (int i = ItemInvStart; i <= ItemInvEnd; i++)
        {
            var slot = slots[i];
            if (slot == null || slot.IsEmpty || slot.item == null) continue;
            if (slot.item.data.key != itemKey) continue;

            total += slot.item.quantity;
            if (total >= amount) return true;
        }

        return false;
    }

    public bool RemoveItem(int itemKey, int amount)
    {
        if (!HasItem(itemKey, amount)) return false;

        int remain = amount;

        for (int i = ItemInvStart; i <= ItemInvEnd; i++)
        {
            var slot = slots[i];
            if (slot == null || slot.IsEmpty || slot.item == null) continue;
            if (slot.item.data.key != itemKey) continue;

            int take = Mathf.Min(slot.item.quantity, remain);
            slot.item.quantity -= take;
            remain -= take;

            if (slot.item.quantity <= 0)
                slot.item = null;

            if (remain <= 0)
            {
                OnInventoryChanged?.Invoke();
                return true;
            }
        }
        return false;
    }

    public void SaveToData(SaveData data)
    {
        data.inventoryItems.Clear();
        for (int i = 0; i < slots.Count; i++)
        {
            InventorySlot slot = slots[i];
            if (slot != null && !slot.IsEmpty && slot.item != null && slot.item.data != null)
            {
                ItemSaveData saveData = new ItemSaveData();
                saveData.slotIndex = i;
                saveData.itemKey = slot.item.data.key;
                saveData.amount = slot.item.quantity;

                data.inventoryItems.Add(saveData);
            }
        }
    }

    public void LoadFromData(SaveData data)
    {
        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i] != null) slots[i].item = null;
        }

        foreach (ItemSaveData savedItem in data.inventoryItems)
        {
            if (savedItem.slotIndex < 0 || savedItem.slotIndex >= slots.Count) continue;

            Data_table itemInfo = GameManager.Instance.Data.Data_TableLoader.GetByKey(savedItem.itemKey);

            if (itemInfo != null)
            {
                ItemInstance newItem = new ItemInstance(itemInfo, savedItem.amount);
                slots[savedItem.slotIndex].item = newItem;

                if (savedItem.slotIndex == EquipWeaponIndex)
                {
                    newItem.equipped = true;
                }
            }
        }
        OnInventoryChanged?.Invoke();
    }
}