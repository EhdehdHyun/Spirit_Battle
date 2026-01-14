using UnityEngine;

public class NPCInteractable : MonoBehaviour, IInteractable
{
    [Header("Dialogue")]
    [SerializeField] private string startDialogueID = "DLG_1001";
    
    [Header("Quest")]
    [SerializeField] private int giveQuestID = -1;
    [SerializeField] private int completeQuestID = -1;

    private bool isTalking;
    public bool canInteract = true;

    private int npcID = -1;

    private void Awake()
    {
        NPCIdentity identity = GetComponent<NPCIdentity>();
        if (identity != null)
        {
            npcID = identity.NPCID;
        }
        else
        {
            Debug.LogError($"{name} : NPCIdentity가 없습니다.");
        }
    }
    // PlayerInteraction에서 조준 중일 때 호출
    public string GetInteractPrompt()
    {
        if (isTalking) return string.Empty;
        return "대화하기 [F]";
    }

    // F 키 눌렀을 때
    public void Interact(PlayerInteraction player)
    {
        if (isTalking) return;

        Debug.Log("NPC INTERACT CALLED");

        isTalking = true;

        DialogueManager.Instance.StartDialogue(
            startDialogueID,
            OnDialogueEnd,
        transform   // NPC Transform 전달
        );
    }

    private void OnDialogueEnd()
    {
        isTalking = false;
        {
            isTalking = false;

            var qm = QuestManager.Instance;
            if (qm == null) return;

            //퀘스트 지급 (Side)
            if (giveQuestID > 0 && !qm.HasQuest(giveQuestID))
            {
                qm.AcceptQuest(giveQuestID, npcID);
                Debug.Log($"퀘스트 제공: {giveQuestID}");
            }
        }
    }
}