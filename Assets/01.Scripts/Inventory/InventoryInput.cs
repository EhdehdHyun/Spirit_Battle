using UnityEngine;
using UnityEngine.InputSystem;

public class InventoryInput : MonoBehaviour
{
    [Header("연결할 오브젝트")]
    [Tooltip("켰다 끌 인벤토리 UI 루트 (예: ItemPage)")]
    public GameObject inventoryUIRoot;

    [Header("Input System")]
    [Tooltip("Player/Inventory 액션을 참조하는 InputActionReference")]
    public InputActionReference inventoryToggleAction;

    private void OnEnable()
    {
        if (inventoryToggleAction != null)
        {
            inventoryToggleAction.action.performed += OnTogglePerformed;
            inventoryToggleAction.action.Enable();
        }
    }

    private void OnDisable()
    {
        if (inventoryToggleAction != null)
        {
            inventoryToggleAction.action.performed -= OnTogglePerformed;
            inventoryToggleAction.action.Disable();
        }
    }

    private void OnTogglePerformed(InputAction.CallbackContext ctx)
    {
        if (inventoryUIRoot == null)
            return;

        bool nextActive = !inventoryUIRoot.activeSelf;
        inventoryUIRoot.SetActive(nextActive);

        Debug.Log($"[InventoryInput] 인벤토리 {(nextActive ? "열림" : "닫힘")}");
    }
}
