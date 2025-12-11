using UnityEngine;

public class MiniMapPlayerArrow : MonoBehaviour
{
    [SerializeField] private Transform player;  
    [SerializeField] private RectTransform arrowRect;  

    private void Update()
    {
        if (player == null || arrowRect == null) return;

        // 플레이어의 Y축 회전만 가져오기
        float rotationY = player.eulerAngles.y;

        // UI 화살표는 Z축 회전을 사용함
        arrowRect.localRotation = Quaternion.Euler(0, 0, -rotationY);
    }
}