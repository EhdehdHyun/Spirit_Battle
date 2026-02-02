using UnityEngine;

public class InventoryToggleController : MonoBehaviour
{
    [Header("Inventory UI Root")]
    public GameObject inventoryRoot;

    [Header("Inventory")]
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
        if (GlobalInputBlocker.IsKeyBlocked(KeyCode.Tab)) return;

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
    }

    private void CloseInventory()
    {
        isInventoryOpen = false;

        ItemActionPopupUI.Instance?.Hide();

        if (inventoryRoot != null)
            inventoryRoot.SetActive(false);

        Time.timeScale = 1f;

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
    }
}
