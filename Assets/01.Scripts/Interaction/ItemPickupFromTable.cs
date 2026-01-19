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

    // ✅ 추가: 줍기 성공 이벤트(상자 등에서 구독)
    public Action onPickedUp;

    private static Data_tableLoader loader;

    private static void EnsureLoader()
    {
        if (loader != null) return;

        try
        {
            loader = new Data_tableLoader();
            Debug.Log("[ItemPickupFromTable] Data_tableLoader 생성 완료");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[ItemPickupFromTable] Data_tableLoader 생성 중 예외 발생: {e.Message}");
            loader = null;
        }
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
            Debug.Log($"[Registry] Register CollectItem target={questTargetId}");
        }
    }

    private void OnDestroy()
    {
        if (questTargetId > 0 && QuestTargetRegistry.Instance != null)
        {
            QuestTargetRegistry.Instance.Unregister(questTargetId, transform);
            Debug.Log($"[Registry] Unregister CollectItem target={questTargetId}");
        }
    }

    public void Interact(PlayerInteraction player)
    {
        if (isCollected) return;

        EnsureLoader();
        if (loader == null)
        {
            Debug.LogError("[ItemPickupFromTable] Data_tableLoader 가 초기화되지 않았습니다. (EnsureLoader 실패)");
            return;
        }

        Data_table data = loader.GetByKey(itemKey);
        if (data == null)
        {
            Debug.LogWarning($"[ItemPickupFromTable] itemKey [{itemKey}] 에 해당하는 아이템을 찾지 못했습니다.");
            return;
        }

        isCollected = true;

        ItemInstance instance = new ItemInstance(data, quantity);
        Debug.Log($"[ItemPickupFromTable] ItemInstance 생성: {instance.data.ItemName} x{instance.quantity}");

        if (InventoryManager.Instance != null)
        {
            Debug.Log("[ItemPickupFromTable] InventoryManager.AddItem 호출 시도");
            InventoryManager.Instance.AddItem(instance);
            
            QuestManager.Instance.ReportProgress(
                CompleteCondition.CollectItem,
                itemKey,
                quantity
            );
        }
        else
        {
            Debug.LogWarning("[ItemPickupFromTable] InventoryManager.Instance 가 없습니다.");
        }

        Debug.Log($"[ItemPickupFromTable] {data.ItemName} x{quantity} 획득 후 인벤토리에 추가 시도");

        // ✅ 여기서 "줍기 완료" 이벤트 발생 (상자가 이걸 듣고 사라짐)
        onPickedUp?.Invoke();

        Destroy(gameObject);
    }

    public string GetInteractPrompt()
    {
        EnsureLoader();
        return "Press [F]";
    }
}
