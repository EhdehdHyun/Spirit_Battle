using UnityEngine;

public class MapController : MonoBehaviour
{
    [SerializeField] private GameObject worldMapPanel;

    void Start()
    {
        if (worldMapPanel != null)
            worldMapPanel.SetActive(false);

        // [주석 처리] 커서 초기화는 이제 GameManager가 함
        // Cursor.lockState = CursorLockMode.Locked;
        // Cursor.visible = false;
    }

    // 이제 M키 입력은 GameManager가 받아서 처리합니다.
    /* void Update()
    {
        if (GlobalInputBlocker.IsKeyBlocked(KeyCode.M)) return;
        if (Input.GetKeyDown(KeyCode.M))
        {
            ToggleMap();
        }
    }
    */

    public void ToggleMap()
    {
        bool isMapActive = !worldMapPanel.activeSelf;
        worldMapPanel.SetActive(isMapActive);

        // [주석 처리] 커서 제어권은 CursorManager에게 넘어갔으므로 여기서 삭제
        /*
        if (isMapActive)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        */

    }
}