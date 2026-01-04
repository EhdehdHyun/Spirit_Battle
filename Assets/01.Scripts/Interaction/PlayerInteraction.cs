using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Raycast")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private float interactDistance = 5f;
    [SerializeField] private LayerMask interactLayerMask;

    [Header("UI")]
    [SerializeField] private Image crosshair;
    [SerializeField] private TextMeshProUGUI interactText;

    private IInteractable currentTarget;

    private bool isLocked;
    private bool waitForFRelease;

    public bool IsWaitingForRelease => waitForFRelease;

    private void Awake()
    {
        if (playerCamera == null)
            playerCamera = Camera.main;

        if (interactText != null)
            interactText.gameObject.SetActive(false);
    }

    private void Update()
    {
        // 대화 종료 후 F 키 릴리즈 대기
        if (waitForFRelease)
        {
            if (Input.GetKeyUp(KeyCode.F))
                waitForFRelease = false;

            return;
        }

        if (isLocked)
        {
            // 대화 중엔 Next만 허용
            if (DialogueManager.Instance != null &&
                DialogueManager.Instance.IsDialogueActive &&
                Input.GetKeyDown(KeyCode.F))
            {
                DialogueManager.Instance.Next();
            }
            return;
        }

        UpdateRaycast();

        if (Input.GetKeyDown(KeyCode.F))
        {
            TryInteract();
        }
    }

    private void UpdateRaycast()
    {
        currentTarget = null;

        Ray ray = playerCamera.ScreenPointToRay(
            new Vector3(Screen.width / 2f, Screen.height / 2f));

        if (Physics.Raycast(ray, out RaycastHit hit, interactDistance, interactLayerMask))
        {
            currentTarget = hit.collider.GetComponentInParent<IInteractable>();
        }

        if (interactText != null)
        {
            interactText.gameObject.SetActive(currentTarget != null);
        }
    }

    private void TryInteract()
    {
        if (isLocked || waitForFRelease)
            return;

        if (currentTarget == null)
            return;

        currentTarget.Interact(this);
    }

    // ================== Dialogue 연동 ==================

    public void LockInteract()
    {
        isLocked = true;
        currentTarget = null;
        if (interactText != null)
            interactText.gameObject.SetActive(false);
    }

    //  대화 종료 시 호출
    public void OnDialogueEnded()
    {
        isLocked = false;
        waitForFRelease = true; // 반드시 F를 떼야 재개 가능
    }
}
