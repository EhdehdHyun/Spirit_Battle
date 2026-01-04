using UnityEngine;
using UnityEngine.UI;

public class MiniMapZoomController : MonoBehaviour
{
    [Header("미니맵 이미지(UI)")]
    [SerializeField] private RectTransform miniMapImage; 

    [Header("미니맵 내부 화살표(플레이어 아이콘)")]
    [SerializeField] private RectTransform playerArrow;
    
    [Header("카메라 버튼 셋팅")]
    [SerializeField] private Camera miniMapCamera;
 
    //시작시 미니맵 기본값 저장
    private Vector2 defaultSizeDelta;
    private Vector2 defaultAnchoredPos;
    private Vector2 defaultArrowPos;
    

    private void Start()
    {
        miniMapCamera.orthographicSize = 68f;
        
        defaultSizeDelta = miniMapImage.sizeDelta;
        defaultAnchoredPos = miniMapImage.anchoredPosition;
        defaultArrowPos = playerArrow.anchoredPosition;
    }
    
    
}