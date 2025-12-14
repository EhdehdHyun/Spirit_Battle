using UnityEngine;

public class MapController : MonoBehaviour
{
    [SerializeField] private GameObject worldMapPanel;

    void Start()
    {
        if (worldMapPanel != null)
            worldMapPanel.SetActive(false);   // 시작 시 꺼두기
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
        bool next = !worldMapPanel.activeSelf;
        worldMapPanel.SetActive(next);
    }
}