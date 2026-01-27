using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

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

        if (waitForFRelease)
        {
            if (Input.GetKeyUp(KeyCode.F) || !Input.GetKey(KeyCode.F))
                waitForFRelease = false;
        }

        UpdateRaycast();

        if (waitForFRelease) return;

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
            Component targetComp = currentTarget as Component;

            if (targetComp != null && targetComp.CompareTag("Teleport"))
            {
                isRegistered = true;
                StopAllCoroutines();
                StartCoroutine(NoticeRoutine());
            }
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
            if (currentTarget != null)
            {
                interactText.gameObject.SetActive(true);
            }
            else
            {
                interactText.gameObject.SetActive(false);
            }
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
        waitForFRelease = Input.GetKey(KeyCode.F);
        RefreshInteraction();
    }

    public void RefreshInteraction()
    {
        currentTarget = null;
        if (interactText != null) interactText.gameObject.SetActive(false);
        UpdateRaycast();
    }
}