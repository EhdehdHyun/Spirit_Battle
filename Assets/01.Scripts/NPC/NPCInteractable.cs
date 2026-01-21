using UnityEngine;

public class NPCInteractable : MonoBehaviour, IInteractable
{
    [System.Serializable]
    public class QuestDialoguePair
    {
        public int questID;
        public string startDialogueID;
    }

    [Header("Quest → Dialogue Mapping")]
    [SerializeField] private QuestDialoguePair[] questDialogues;
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
    private void PreAdvanceIfFinished()
    {
        var qm = QuestManager.Instance;
        if (qm == null) return;

        if (giveQuestID <= 0) return;

        var state = qm.GetQuestState(giveQuestID);

        // 완료/보상수령 상태면 다음 단계로 미리 전환
        if (state == QuestState.Completed || state == QuestState.RewardClaimed)
        {
            var quest = qm.GetQuestData(giveQuestID);
            if (quest != null && quest.NextQuest > 0)
            {
                giveQuestID = quest.NextQuest;
                startDialogueID = GetStartDialogueForQuest(giveQuestID);

                // 다음 퀘스트는 지금 말걸기에서 바로 지급
                if (!qm.HasQuest(giveQuestID))
                {
                    qm.AcceptQuest(giveQuestID, npcID);
                    Debug.Log($"다음 퀘스트 즉시 제공(대화 시작 전): {giveQuestID}");
                }
            }
        }
    }

    // F 키 눌렀을 때
    public void Interact(PlayerInteraction player)
    {
        if (isTalking) return;
        PreAdvanceIfFinished();

        Debug.Log("NPC INTERACT CALLED");
        isTalking = true;

        DialogueManager.Instance.StartDialogue(
            startDialogueID,
            OnDialogueEnd,
            transform
        );
    }
    private string GetStartDialogueForQuest(int questID)
    {
        foreach (var pair in questDialogues)
        {
            if (pair.questID == questID)
                return pair.startDialogueID;
        }

        Debug.LogWarning($"퀘스트 {questID}에 대한 Dialogue가 없습니다.");
        return startDialogueID; // fallback
    }
    private void OnDialogueEnd()
    {
        isTalking = false;

        var qm = QuestManager.Instance;
        if (qm == null) return;

        // 1.보고 / 전달 퀘스트 처리 (있으면 여기서 끝)
        if (qm.TryCompleteTalkToNPCQuest(npcID))
        {
            // 보고가 우선이므로 새 퀘스트는 주지 않음
            return;
        }

        // 2.아직 안 받은 퀘스트면 지급
        if (giveQuestID > 0 && !qm.HasQuest(giveQuestID))
        {
            qm.AcceptQuest(giveQuestID, npcID);
            Debug.Log($"퀘스트 제공: {giveQuestID}");
        }
    }

}