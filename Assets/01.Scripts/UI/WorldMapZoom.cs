using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class WorldMapZoom : MonoBehaviour, IScrollHandler, IDragHandler
{
    [SerializeField] private RectTransform mapImage;
    [SerializeField] private float zoomSpeed = 0.3f;
    [SerializeField] private float minZoom = 0.3f;
    [SerializeField] private float maxZoom = 3f;

    public void OnScroll(PointerEventData eventData)
    {
        float current = mapImage.localScale.x;
        current += eventData.scrollDelta.y * zoomSpeed;
        current = Mathf.Clamp(current, minZoom, maxZoom);
        mapImage.localScale = new Vector3(current, current, 1);
    }

    public void OnDrag(PointerEventData eventData)
    {
        mapImage.anchoredPosition += eventData.delta;
    }
}