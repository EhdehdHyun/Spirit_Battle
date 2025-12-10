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

        if (TempInventory.Instance != null)
        {
            TempInventory.Instance.AddItem(data, quantity);
        }
        else
        {
            Debug.LogWarning("[ItemPickupFromTable] TempInventory.Instance 가 없음. 인벤토리에 저장되지 않음.");
        }

        Debug.Log($"[ItemPickupFromTable] {data.ItemName} x{quantity} 획득 후 인벤토리에 추가");

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
