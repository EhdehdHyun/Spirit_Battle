using System;
using UnityEngine;

public class ItemPickupFromTable : MonoBehaviour, IInteractable
{
    [Header("Data_table 의 key (엑셀/JSON 아이템 ID)")]
    public int itemKey;
    public int quantity = 1;
    [Header("Quest Target (HUD 거리 표시용)")]
    [SerializeField] private int questTargetId; 

    private bool isCollected = false;

    public Action onPickedUp;

    private static Data_tableLoader loader;

    private static void EnsureLoader()
    {
        if (loader != null) return;
            loader = new Data_tableLoader();
    }

    private void Awake()
    {
        EnsureLoader();
    }
    
    private void OnEnable()
    {
        if (questTargetId > 0 && QuestTargetRegistry.Instance != null)
        {
            QuestTargetRegistry.Instance.Register(questTargetId, transform);
        }
    }

    private void OnDestroy()
    {
        if (questTargetId > 0 && QuestTargetRegistry.Instance != null)
        {
            QuestTargetRegistry.Instance.Unregister(questTargetId, transform);
        }
    }

    public void Interact(PlayerInteraction player)
    {
        if (isCollected) return;

        EnsureLoader();
        if (loader == null)
        {
            return;
        }

        Data_table data = loader.GetByKey(itemKey);
        if (data == null)
        {
            return;
        }

        isCollected = true;

        ItemInstance instance = new ItemInstance(data, quantity);

        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.AddItem(instance);
            
            QuestManager.Instance.ReportProgress(
                CompleteCondition.CollectItem,
                itemKey,
                quantity
            );
        }
        else
        {
        }
        onPickedUp?.Invoke();

        Destroy(gameObject);
    }

    public string GetInteractPrompt()
    {
        EnsureLoader();
        return "Press [F]";
    }
}
