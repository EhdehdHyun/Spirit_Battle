using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections; // 코루틴 사용을 위해 추가

public class PlayerInteraction : MonoBehaviour
{
    [Header("Raycast")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private float interactDistance = 5f;
    [SerializeField] private LayerMask interactLayerMask;

    [Header("UI")]
    [SerializeField] private Image crosshair;
    [SerializeField] private TextMeshProUGUI interactText;
    [SerializeField] private GameObject noticeUI;

    private IInteractable currentTarget;

    private bool isLocked;
    private bool waitForFRelease;
    private bool isRegistered = false;

    public bool IsWaitingForRelease => waitForFRelease;

    private void Awake()
    {
        if (playerCamera == null)
            playerCamera = Camera.main;

        if (interactText != null)
            interactText.gameObject.SetActive(false);

        if (noticeUI != null)
            noticeUI.SetActive(false);
    }

    private void Update()
    {
        if (waitForFRelease)
        {
            if (Input.GetKeyUp(KeyCode.F))
                waitForFRelease = false;

            return;
        }

        if (isLocked)
        {
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
            HandleFirstNotice();
        }
    }

    private void HandleFirstNotice()
    {
        if (!isRegistered && currentTarget != null)
        {
            isRegistered = true;
            StopAllCoroutines();
            StartCoroutine(NoticeRoutine());
        }
    }
    IEnumerator NoticeRoutine()
    {
        if (noticeUI != null) noticeUI.SetActive(true);
        yield return new WaitForSeconds(2f);
        if (noticeUI != null) noticeUI.SetActive(false);
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


    public void LockInteract()
    {
        isLocked = true;
        currentTarget = null;
        if (interactText != null)
            interactText.gameObject.SetActive(false);
    }

    public void OnDialogueEnded()
    {
        isLocked = false;
        waitForFRelease = true;
    }
}