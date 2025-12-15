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
}