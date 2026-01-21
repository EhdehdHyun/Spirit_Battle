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
            loader = new Data_tableLoader();;
        }
        catch (System.Exception e)
        {
            loader = null;
        }
    }

    private void Awake()
    {
        EnsureLoader();
    }

    public void Interact(PlayerInteraction player)
    {
        if (isOpened) return;
        isOpened = true;

        EnsureLoader();
        if (loader == null)
        {
            return;
        }

        Data_table data = loader.GetByKey(itemKey);
        if (data == null)
        {
        }
        else
        {
            ItemInstance inst = new ItemInstance(data, amount);

            if (InventoryManager.Instance != null)
            {
                InventoryManager.Instance.AddItem(inst);
            }
            else
            {
            }
        }

        if (coinPrefab != null)
        {
            Vector3 pos = spawnPoint != null
                ? spawnPoint.position
                : transform.position + Vector3.up * 1.0f;

            Instantiate(coinPrefab, pos, Quaternion.identity);
        }

        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;
    }

    public string GetInteractPrompt()
    {
        return isOpened ? "" : "F : 상자 열기";
    }
}
