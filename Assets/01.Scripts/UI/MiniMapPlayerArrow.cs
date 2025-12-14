using UnityEngine;

public class MiniMapPlayerArrow : MonoBehaviour
{
    [SerializeField] private Transform player;  
    [SerializeField] private RectTransform arrowRect;  

    private void Update()
    {
        if (player == null || arrowRect == null) return;
        
        Vector3 dir = player.right; 

        // 북쪽(+Z) 기준 각도 계산
        float angle = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;

        // UI는 Z축 회전
        arrowRect.localRotation = Quaternion.Euler(0f, 0f, -angle);
    }
}