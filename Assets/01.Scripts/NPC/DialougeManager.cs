using UnityEngine;
using TMPro;
using System;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }
    public bool IsDialogueActive { get; private set; }

    [Header("UI")]
    [SerializeField] private GameObject dialogueCanvas;
    [SerializeField] private TextMeshProUGUI dialogueText;

    [Header("Data")]
    [SerializeField] private DataManager dataManager;

    [SerializeField] private DialogueCameraController dialogueCamera;

    [Header("Lock Targets")]
    [SerializeField] private MonoBehaviour playerMovement;
    [SerializeField] private NPCFaceController npcFaceController;
    [SerializeField] private Transform playerTransform;

    private string currentDialogueID;
    private Action onDialogueEnd;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (dataManager == null)
        {
            dataManager = new DataManager();
            dataManager.Initialize();
        }

        IsDialogueActive = false;

        if (dialogueCanvas != null)
            dialogueCanvas.SetActive(false);
    }

    public void StartDialogue(string startID, Action onEnd, Transform npcTransform)
    {
        if (IsDialogueActive) return; // 이미 대화중이면 무시(중복 방지)

        Debug.Log($"[DialogueManager] StartDialogue : {startID}");

        IsDialogueActive = true;
        currentDialogueID = startID;
        onDialogueEnd = onEnd;

        // ✅ 대화 시작 시 일시정지
        GamePause.Request(this);

        // 커서/락 (대화 중엔 UI 조작)
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // 입력 잠금
        if (playerMovement != null)
            playerMovement.enabled = false;

        if (dialogueCanvas != null)
            dialogueCanvas.SetActive(true);

        if (dialogueCamera != null)
            dialogueCamera.StartDialogueCamera(npcTransform);

        npcFaceController = npcTransform != null ? npcTransform.GetComponent<NPCFaceController>() : null;
        if (npcFaceController != null && playerTransform != null)
            npcFaceController.LookAtTarget(playerTransform);

        ShowCurrent();
    }

    public void Next()
    {
        if (!IsDialogueActive) return;

        var data = dataManager.DialogueTableLoader.GetDialogue(currentDialogueID);

        if (data == null || string.IsNullOrEmpty(data.NextID))
        {
            EndDialogue();
            return;
        }

        currentDialogueID = data.NextID;
        ShowCurrent();
    }

    private void ShowCurrent()
    {
        var data = dataManager.DialogueTableLoader.GetDialogue(currentDialogueID);

        if (data == null)
        {
            Debug.LogError($"[DialogueManager] Dialogue 데이터 없음 : {currentDialogueID}");
            return;
        }

        Debug.Log($"[DialogueManager] Show : {data.Text}");

        if (dialogueText != null)
        {
            dialogueText.text = data.Text;
            dialogueText.gameObject.SetActive(true);
        }
    }

    private void EndDialogue()
    {
        Debug.Log("EndDialogue CALLED");

        IsDialogueActive = false;

        if (dialogueText != null)
        {
            dialogueText.text = "";
            dialogueText.gameObject.SetActive(false);
        }

        if (dialogueCanvas != null)
            dialogueCanvas.SetActive(false);

        // 입력 잠금 해제
        if (playerMovement != null)
            playerMovement.enabled = true;

        if (dialogueCamera != null)
            dialogueCamera.EndDialogueCamera();

        if (npcFaceController != null)
            npcFaceController.StopLook();

        // ✅ 대화 종료 시 일시정지 해제
        GamePause.Release(this);

        // 커서 원복(게임 조작)
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        onDialogueEnd?.Invoke();
        onDialogueEnd = null;
    }
}
