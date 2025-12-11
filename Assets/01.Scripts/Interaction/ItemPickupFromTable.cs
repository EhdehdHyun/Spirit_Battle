using UnityEngine;

public class ItemPickupFromTable : MonoBehaviour, IInteractable
{
    [Header("Data_table 의 key (엑셀/JSON 아이템 ID)")]
    public int itemKey;
    public int quantity = 1;

    private bool isCollected = false;

    private static Data_tableLoader loader;

    private static void EnsureLoader()
    {
        if (loader != null) return;

        try
        {
            loader = new Data_tableLoader();   // "JSON/Data_table" 사용
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

    public void Interact()
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

        // 2) ItemInstance 생성
        ItemInstance instance = new ItemInstance(data, quantity);
        Debug.Log($"[ItemPickupFromTable] ItemInstance 생성: {instance.data.ItemName} x{instance.quantity}");

        // 3) 인벤토리 매니저에 추가 (ItemInstance 기반)
        if (InventoryManager.Instance != null)
        {
            Debug.Log("[ItemPickupFromTable] InventoryManager.AddItem 호출 시도");
            InventoryManager.Instance.AddItem(instance);
        }
        else
        {
            Debug.LogWarning("[ItemPickupFromTable] InventoryManager.Instance 가 없습니다.");
        }

        Debug.Log($"[ItemPickupFromTable] {data.ItemName} x{quantity} 획득 후 인벤토리에 추가 시도");

        Destroy(gameObject);
    }


    public string GetInteractPrompt()
    {
        EnsureLoader();

        if (loader == null)
            return "F : 아이템 줍기";

        Data_table data = loader.GetByKey(itemKey);
        if (data == null)
            return "F : 알 수 없는 아이템";

        return $"F : {data.ItemName} 줍기";
    }
}
