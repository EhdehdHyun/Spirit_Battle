using UnityEngine;

public class InventoryToggleController : MonoBehaviour
{
    [Header("Inventory UI Root")]
    public GameObject inventoryRoot;

    [Header("인벤 열렸을 때 꺼둘 플레이어 쪽 스크립트들")]
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

        // 인벤토리 UI 켜기
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

        Debug.Log("[InventoryToggleController] 인벤토리 열기");
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

        Debug.Log("[InventoryToggleController] 인벤토리 닫기");
    }
}
