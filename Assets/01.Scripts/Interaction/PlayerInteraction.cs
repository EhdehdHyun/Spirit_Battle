using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Raycast 설정")]
    [SerializeField] public Camera playerCamera;
    [SerializeField] private float interactDistance = 5f;
    [SerializeField] private LayerMask interactLayerMask;

    [Header("UI")]
    [SerializeField] private Image crosshairImage;          // 크로스헤어 이미지
    [SerializeField] private Color crosshairNormalColor = Color.white;
    [SerializeField] private Color crosshairInteractColor = Color.red;
    [SerializeField] private TextMeshProUGUI interactText;  // "F키를 눌러 상호작용" 텍스트

    private IInteractable currentTarget;

    private void Awake()
    {
        // 카메라 안 넣어놨으면 자동으로 MainCamera 찾기
        if (playerCamera == null && Camera.main != null)
            playerCamera = Camera.main;

        // 시작할 때 텍스트는 꺼두기
        if (interactText != null)
            interactText.gameObject.SetActive(false);
    }

    private void Update()
    {
        UpdateTargetByRaycast();

        if (Input.GetKeyDown(KeyCode.F))
        {
            Debug.Log("[PlayerInteraction] F 키 입력 감지 (Raycast 버전)");
            TryInteract();
        }
    }

    /// <summary>
    /// 화면 중앙에서 레이캐스트 쏴서 상호작용 대상 찾기
    /// </summary>
    private void UpdateTargetByRaycast()
    {
        currentTarget = null;

        if (playerCamera == null)
            return;

        Vector3 screenCenter = new Vector3(Screen.width / 2f, Screen.height / 2f);
        Ray ray = playerCamera.ScreenPointToRay(screenCenter);

        if (Physics.Raycast(ray, out RaycastHit hit, interactDistance, interactLayerMask))
        {
            var target = hit.collider.GetComponentInParent<IInteractable>();
            if (target != null)
            {
                currentTarget = target;
                Debug.DrawLine(ray.origin, hit.point, Color.red);
            }
        }

        UpdateUI();
    }

    /// <summary>
    /// 크로스헤어 색 + 상호작용 텍스트 갱신
    /// </summary>
    private void UpdateUI()
    {
        // 크로스헤어 색 변경
        if (crosshairImage != null)
        {
            crosshairImage.color = (currentTarget != null)
                ? crosshairInteractColor
                : crosshairNormalColor;
        }

        // "F키를 눌러 상호작용" 텍스트
        if (interactText != null)
        {
            if (currentTarget != null)
            {
                // IInteractable 이 개별 문구를 주면 그걸 우선 사용
                string prompt = currentTarget.GetInteractPrompt();

                if (string.IsNullOrEmpty(prompt))
                    prompt = "Press [F]";

                interactText.text = prompt;
                interactText.gameObject.SetActive(true);
            }
            else
            {
                interactText.gameObject.SetActive(false);
            }
        }
    }

    /// <summary>
    /// 실제 상호작용 실행
    /// </summary>
    private void TryInteract()
    {
        if (currentTarget == null)
        {
            Debug.Log("[PlayerInteraction] TryInteract: currentTarget 없음");
            return;
        }

        Debug.Log($"[PlayerInteraction] {currentTarget} 에 상호작용 시도");
        currentTarget.Interact(this);
    }
}
