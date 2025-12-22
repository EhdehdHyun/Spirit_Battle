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

        if (dialogueCanvas != null)
            dialogueCanvas.SetActive(false);
    }

    public void StartDialogue(string startID, Action onEnd, Transform npcTransform)
    {
        Debug.Log($"[DialogueManager] StartDialogue : {startID}");
        
        IsDialogueActive = true;

        currentDialogueID = startID;
        onDialogueEnd = onEnd;
        
        // 🔒 입력 잠금
        if (playerMovement != null)
            playerMovement.enabled = false;

        dialogueCanvas.SetActive(true);
        
        dialogueCamera.StartDialogueCamera(npcTransform);
        
        npcFaceController = npcTransform.GetComponent<NPCFaceController>();
        if (npcFaceController != null)
            npcFaceController.LookAtTarget(playerTransform);

        ShowCurrent();
    }

    public void Next()
    {
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

        dialogueText.text = data.Text;
        dialogueText.gameObject.SetActive(true);
    }

    private void EndDialogue()
    {
        IsDialogueActive = false;

        dialogueText.text = "";
        dialogueText.gameObject.SetActive(false);
        dialogueCanvas.SetActive(false);
        
        if (playerMovement != null)
            playerMovement.enabled = true;
        
        if (dialogueCamera != null)
            dialogueCamera.EndDialogueCamera();
        
        if (npcFaceController != null)
            npcFaceController.StopLook();        

        onDialogueEnd?.Invoke();
        onDialogueEnd = null;
        
        //if (TutorialManager.Instance != null)
            //TutorialManager.Instance.StartTutorial();
    }
}
