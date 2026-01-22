using UnityEngine;

public class ChestSpawnCoin : MonoBehaviour, IInteractable
{
    [Header("���ڿ��� �� ������ (Data_table key)")]
    public int itemKey = 1001;      // �ӽ÷� HP Potion ���� ��
    public int amount = 1;          // �� ���� �� ����

    [Header("����� ���� ������ (����)")]
    public GameObject coinPrefab;
    public Transform spawnPoint;

    [Header("����")]
    public bool isOpened = false;

    private static Data_tableLoader loader;

    private static void EnsureLoader()
    {
        if (loader != null) return;
        loader = new Data_tableLoader();;
    
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
        return isOpened ? "" : "F : ���� ����";
    }
}
