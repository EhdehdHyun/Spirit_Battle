using UnityEngine;

public class MapController : MonoBehaviour
{
    [SerializeField] private GameObject worldMapPanel;

    void Start()
    {
        if (worldMapPanel != null)
            worldMapPanel.SetActive(false);   // 시작 시 꺼두기

        // 게임 시작 시 마우스 잠금 (필요하다면)
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.M))
        {
            ToggleMap();
        }
    }

    private void ToggleMap()
    {
        bool isMapActive = !worldMapPanel.activeSelf;
        worldMapPanel.SetActive(isMapActive);

        if (isMapActive)
        {
            // 지도가 켜짐 -> 마우스 잠금 해제 및 보이기
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            // 지도가 꺼짐 -> 마우스 다시 잠금 및 숨기기
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}