using UnityEngine;

public class CursorManager : MonoBehaviour
{
    public static CursorManager Instance;

    private int activeUICount = 0;
    private bool isAltMode = false;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.LeftAlt))
        {
            isAltMode = !isAltMode;
        }

        UpdateCursorState();
    }

    public void SetUIActive(bool isOpen)
    {
        if (isOpen)
            activeUICount++;
        else
            activeUICount--;

        if (activeUICount < 0) activeUICount = 0;
    }

    private void UpdateCursorState()
    {
        bool shouldUnlock = (activeUICount > 0) || isAltMode;

        if (shouldUnlock)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}