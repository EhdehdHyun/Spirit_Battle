using UnityEngine;

public class CursorLocker : MonoBehaviour
{
    private void OnEnable()
    {
        Lock();
    }

    private void OnDisable()
    {
        Unlock();
    }

    public void Lock()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void Unlock()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}
