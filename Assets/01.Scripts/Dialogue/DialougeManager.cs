using UnityEngine;
using TMPro;
using System;
using System.Collections;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }
    public bool IsDialogueActive { get; private set; }

    [Header("UI")]
    [SerializeField] private GameObject dialogueCanvas;
    [SerializeField] private TextMeshProUGUI dialogueText;

    [Header("Data")]
    [SerializeField] private DataManager dataManager;

    [Header("Camera")]
    [SerializeField] private DialogueCameraController dialogueCamera;

    [Header("Lock Targets")]
    [SerializeField] private MonoBehaviour playerMovement;
    [SerializeField] private Transform playerTransform;

    private PlayerInteraction playerInteraction;
    private NPCFaceController npcFaceController;

    private string currentDialogueID;
    private Action onDialogueEnd;
    private bool blockInputAtStart;

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

        if (dialogueCanvas != null)
            dialogueCanvas.SetActive(false);

        playerInteraction = FindObjectOfType<PlayerInteraction>();
    }

    //외부에서 호출하는 진입점
    public void StartDialogue(string startID, Action onEnd, Transform npcTransform, bool autoStart = false)
    {
        if (IsDialogueActive) return;
        StartCoroutine(StartDialogueRoutine(startID, onEnd, npcTransform, autoStart));
    }

    //실제 시작 로직
    private IEnumerator StartDialogueRoutine(string startID, Action onEnd, Transform npcTransform, bool autoStart)
    {
        Debug.Log($"[DialogueManager] StartDialogue : {startID}");

        IsDialogueActive = true;
        currentDialogueID = startID;
        onDialogueEnd = onEnd;
        blockInputAtStart = autoStart;

        playerInteraction.LockInteract();

        GamePause.Request(this);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (playerMovement != null)
            playerMovement.enabled = false;

        if (dialogueCanvas != null)
            dialogueCanvas.SetActive(true);

        // 카메라 연출 끝까지 대기
        if (dialogueCamera != null)
            yield return dialogueCamera.StartDialogueCameraRoutine(npcTransform);

        // 자동 대화 입력 보호 해제
        blockInputAtStart = false;

        npcFaceController = npcTransform.GetComponent<NPCFaceController>();
        if (npcFaceController != null && playerTransform != null)
            npcFaceController.LookAtTarget(playerTransform);

        ShowCurrent();
    }

    public void Next()
    {
        if (!IsDialogueActive || blockInputAtStart)
            return;

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
        if (data == null) return;

        dialogueText.text = data.Text;
        dialogueText.gameObject.SetActive(true);
    }

    private void EndDialogue()
    {
        StartCoroutine(EndDialogueRoutine());
    }

    private IEnumerator EndDialogueRoutine()
    {
        IsDialogueActive = false;

        dialogueText.text = "";
        dialogueText.gameObject.SetActive(false);
        dialogueCanvas.SetActive(false);

        npcFaceController?.StopLook();

        GamePause.Release(this);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (dialogueCamera != null)
            yield return dialogueCamera.EndDialogueCameraRoutine();

        playerInteraction.OnDialogueEnded();

        if (playerMovement != null)
            playerMovement.enabled = true;

        onDialogueEnd?.Invoke();
        onDialogueEnd = null;
    }
}
