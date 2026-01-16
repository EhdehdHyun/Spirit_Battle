using UnityEngine;

public class InventoryToggleController : MonoBehaviour
{
    [Header("Inventory UI Root")]
    public GameObject inventoryRoot;

    [Header("�κ� ������ �� ���� �÷��̾� �� ��ũ��Ʈ��")]
    public MonoBehaviour[] gameplayScriptsToDisable;

    private bool isInventoryOpen = false;
    private float previousTimeScale = 1f;

    private void Start()
    {
        if (inventoryRoot != null)
            inventoryRoot.SetActive(false);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        if (GlobalInputBlocker.BlockKeyboard) return;

        if (Input.GetKeyDown(KeyCode.Tab))
        {
            if (isInventoryOpen)
                CloseInventory();
            else
                OpenInventory();
        }
    }

    private void OpenInventory()
    {
        isInventoryOpen = true;

        // �κ��丮 UI �ѱ�
        if (inventoryRoot != null)
            inventoryRoot.SetActive(true);

        previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (gameplayScriptsToDisable != null)
        {
            foreach (var comp in gameplayScriptsToDisable)
            {
                if (comp != null)
                    comp.enabled = false;
            }
        }

        Debug.Log("[InventoryToggleController] �κ��丮 ����");
    }

    private void CloseInventory()
    {
        isInventoryOpen = false;

        ItemActionPopupUI.Instance?.Hide();

        if (inventoryRoot != null)
            inventoryRoot.SetActive(false);

        Time.timeScale = previousTimeScale;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (gameplayScriptsToDisable != null)
        {
            foreach (var comp in gameplayScriptsToDisable)
            {
                if (comp != null)
                    comp.enabled = true;
            }
        }

        Debug.Log("[InventoryToggleController] �κ��丮 �ݱ�");
    }
}
