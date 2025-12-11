using UnityEngine;

public class ChestSpawnCoin : MonoBehaviour, IInteractable
{
    [Header("상자에서 줄 아이템 (Data_table key)")]
    public int itemKey = 1001;      // 임시로 HP Potion 같은 것
    public int amount = 1;          // 한 번에 줄 개수

    [Header("연출용 코인 프리팹 (선택)")]
    public GameObject coinPrefab;
    public Transform spawnPoint;

    [Header("상태")]
    public bool isOpened = false;

    private static Data_tableLoader loader;

    private static void EnsureLoader()
    {
        if (loader != null) return;

        try
        {
            loader = new Data_tableLoader();   // "JSON/Data_table"
            Debug.Log("[ChestSpawnCoin] Data_tableLoader 생성 완료");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[ChestSpawnCoin] Data_tableLoader 생성 중 예외 발생: {e.Message}");
            loader = null;
        }
    }

    private void Awake()
    {
        EnsureLoader();
    }

    public void Interact()
    {
        if (isOpened) return;
        isOpened = true;

        EnsureLoader();
        if (loader == null)
        {
            Debug.LogError("[ChestSpawnCoin] Data_tableLoader 가 없음");
            return;
        }

        // 1) 테이블에서 데이터 가져오기
        Data_table data = loader.GetByKey(itemKey);
        if (data == null)
        {
            Debug.LogWarning($"[ChestSpawnCoin] itemKey [{itemKey}] 에 해당하는 아이템을 찾지 못했습니다.");
        }
        else
        {
            // 2) ItemInstance 생성 후 인벤토리에 추가
            ItemInstance inst = new ItemInstance(data, amount);

            if (InventoryManager.Instance != null)
            {
                InventoryManager.Instance.AddItem(inst);
                Debug.Log($"[ChestSpawnCoin] {data.ItemName} x{amount} 인벤토리에 추가됨");
            }
            else
            {
                Debug.LogWarning("[ChestSpawnCoin] InventoryManager.Instance 가 없습니다.");
            }
        }

        // 3) 연출용 코인 프리팹 스폰 (선택)
        if (coinPrefab != null)
        {
            Vector3 pos = spawnPoint != null
                ? spawnPoint.position
                : transform.position + Vector3.up * 1.0f;

            Instantiate(coinPrefab, pos, Quaternion.identity);
        }

        // 4) 다시 상호작용 안 되게 Collider 끄기
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        // 필요하면 Destroy(gameObject); 로 상자를 없애도 됨.
    }

    public string GetInteractPrompt()
    {
        return isOpened ? "" : "F : 상자 열기";
    }
}
