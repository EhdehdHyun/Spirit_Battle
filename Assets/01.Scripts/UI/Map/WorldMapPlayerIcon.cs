using UnityEngine;

public class WorldMapPlayerIcon : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform mapImage;
    [SerializeField] private RectTransform playerIcon;
    [SerializeField] private Transform player;

    [Header("World Bounds")]
    [SerializeField] private Vector2 worldMin;
    [SerializeField] private Vector2 worldMax;

    private void Update()
    {
        UpdatePlayerIcon();
        UpdatePlayerRotation();
    }

    private void UpdatePlayerIcon()
    {
        Vector3 p = player.position;

        float nx = Mathf.InverseLerp(worldMin.x, worldMax.x, p.x);
        float ny = Mathf.InverseLerp(worldMin.y, worldMax.y, p.z); 

        float mapX = (nx - 0.5f) * mapImage.rect.width;
        float mapY = (ny - 0.5f) * mapImage.rect.height;

        playerIcon.anchoredPosition = new Vector2(mapX, mapY);
    }
    private void UpdatePlayerRotation()
    {
        Vector3 forward = player.forward;

        // Z가 위, X가 오른쪽 기준
        float angle = Mathf.Atan2(forward.z, forward.x) * Mathf.Rad2Deg;

        // 아이콘 기본 방향이 왼쪽
        float uiRotation = angle + 180f;

        playerIcon.localRotation = Quaternion.Euler(0f, 0f, uiRotation);
    }
}