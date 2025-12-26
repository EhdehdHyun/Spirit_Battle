using UnityEngine;
using TMPro;

public class NPCNameUI : MonoBehaviour
{
    public Transform target;     // NPC HeadPoint
    public Transform player;     // Player
    public Camera worldCamera;

    [Header("Optimize")]
    [SerializeField] float showDistance = 10f;
    [SerializeField] int frameSkip = 2;

    RectTransform rect;
    CanvasGroup cg;

    void Awake()
    {
        rect = GetComponent<RectTransform>();

        cg = GetComponent<CanvasGroup>();
        if (cg == null)
            cg = gameObject.AddComponent<CanvasGroup>();
    }

    void LateUpdate()
    {
        // 프레임 스킵
        if (Time.frameCount % frameSkip != 0)
            return;

        if (target == null || player == null || worldCamera == null)
        {
            cg.alpha = 0f;
            return;
        }

        // 거리 체크 
        float sqrDist = (player.position - target.position).sqrMagnitude;
        if (sqrDist > showDistance * showDistance)
        {
            cg.alpha = 0f;
            return;
        }

        Vector3 screenPos = worldCamera.WorldToScreenPoint(target.position);

        if (screenPos.z < 0)
        {
            cg.alpha = 0f;
            return;
        }
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rect.parent as RectTransform,
            screenPos,
            null, // Screen Space - Overlay
            out Vector2 localPos
        );

        cg.alpha = 1f;
        rect.localPosition = localPos;
    }
}