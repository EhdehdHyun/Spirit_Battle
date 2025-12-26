using System.Collections;
using UnityEngine;

public class PrologueStartController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform npcTransform;
    [SerializeField] private DialogueManager dialogueManager;
    [SerializeField] private DialogueCameraController dialogueCamera;
    [SerializeField] private MonoBehaviour playerMovement;

    private const string PROLOGUE_KEY = "ProloguePlayed";

    private void Start()
    {
        if (PlayerPrefs.GetInt(PROLOGUE_KEY, 0) == 1)
            return; // 이미 봤으면 아무것도 안 함

        StartCoroutine(PrologueSequence());
    }

    IEnumerator PrologueSequence()
    {
        playerMovement.enabled = false;

        yield return new WaitForSeconds(0.5f); // 눈 깜빡임 끝

        // 1NPC 쪽으로 자연스럽게 시선 이동
        dialogueCamera.FocusOnce(npcTransform, 0.8f);

        yield return new WaitForSeconds(0.8f);

        // 2대화 카메라로 전환
        dialogueCamera.StartDialogueCamera(npcTransform);

        DialogueManager.Instance.StartDialogue(
            "DLG_1001",
            OnDialogueEnd,
            npcTransform
        );
    }

    private void OnDialogueEnd()
    {
        dialogueCamera.EndDialogueCamera();
        playerMovement.enabled = true;
    }
}