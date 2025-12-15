using UnityEngine;

/// <summary>
/// 화면 중앙에서 Ray를 쏴서 IInteractable을 찾는 상호작용 스크립트
/// </summary>
public class PlayerInteraction : MonoBehaviour
{
    [Header("Raycast 설정")]
    [Tooltip("상호작용 기준이 될 카메라 (보통 플레이어 카메라)")]
    public Camera playerCamera;

    [Tooltip("상호작용 가능한 최대 거리")]
    public float interactDistance = 20f;

    public LayerMask dropLayerMask;
    public float defaultDropDistance = 3f;
    private void Awake()
    {
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
            if (playerCamera == null)
            {
                Debug.LogWarning("[PlayerInteraction] playerCamera 를 찾지 못했습니다.");
            }
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F))
        {
            Debug.Log("[PlayerInteraction] F 키 입력 감지 (Raycast 버전)");
            TryInteract();
        }
    }

    private void TryInteract()
    {
        if (playerCamera == null)
        {
            Debug.LogWarning("[PlayerInteraction] playerCamera 가 없습니다. 상호작용 실패");
            return;
        }

        // ★ 화면 정중앙 픽셀 기준으로 Ray 생성
        Vector3 screenCenter = new Vector3(Screen.width / 2f, Screen.height / 2f, 0f);
        Ray ray = playerCamera.ScreenPointToRay(screenCenter);

        RaycastHit hit;

        // 레이어 마스크, 트리거 이런 거 다 빼고 제일 단순하게
        bool hitSomething = Physics.Raycast(ray, out hit, interactDistance);

        // Scene 뷰에서 레이 확인용
        Debug.DrawRay(ray.origin, ray.direction * interactDistance, Color.red, 0.5f);

        if (!hitSomething)
        {
            Debug.Log("[PlayerInteraction] Raycast: 아무것도 맞지 않음 (ScreenPointToRay)");
            return;
        }

        Debug.Log($"[PlayerInteraction] Raycast hit: {hit.collider.name} (Layer: {LayerMask.LayerToName(hit.collider.gameObject.layer)})");

        // 맞은 오브젝트에서 IInteractable 찾기
        IInteractable interactable = hit.collider.GetComponent<IInteractable>()
                               ?? hit.collider.GetComponentInParent<IInteractable>();

        if (interactable != null)
        {
            Debug.Log($"[PlayerInteraction] {hit.collider.name} 에 상호작용 Interact() 호출");
            interactable.Interact();
        }
        else
        {
            Debug.Log($"[PlayerInteraction] hit 되었지만 IInteractable 이 없음: {hit.collider.name}");
        }
    }

    public Vector3 GetDropPoint()
    {
        if (playerCamera == null)
            return transform.position + transform.forward * defaultDropDistance;

        Vector3 screenCenter = new Vector3(Screen.width / 2f, Screen.height / 2f, 0f);
        Ray ray = playerCamera.ScreenPointToRay(screenCenter);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, defaultDropDistance * 3f, dropLayerMask))
        {
            return hit.point + Vector3.up * 0.1f;
        }
        return ray.origin + ray.direction * defaultDropDistance;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (playerCamera == null) return;

        Vector3 screenCenter = new Vector3(Screen.width / 2f, Screen.height / 2f, 0f);
        Ray ray = playerCamera.ScreenPointToRay(screenCenter);

        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(ray.origin, ray.direction * interactDistance);
    }
#endif
}
