using UnityEngine;

public class NPCInteractable : MonoBehaviour, IInteractable
{
    [Header("Dialogue")]
    [SerializeField] private string startDialogueID = "DLG_1001";

    private bool isTalking;
    public bool canInteract = true;

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
    }
}