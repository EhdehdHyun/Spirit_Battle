using System.Collections;
using UnityEngine;

public class PrologueStartController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform npcTransform;
    [SerializeField] private DialogueManager dialogueManager;
    [SerializeField] private DialogueCameraController dialogueCamera;
    [SerializeField] private MonoBehaviour playerMovement;

    // NPC의 상호작용 스크립트 연결 (F키 제어용)
    [SerializeField] private NPCInteractable npcInteractable;

    private IEnumerator Start()
    {
        // 1. GameManager가 데이터를 불러올 때까지 1프레임 대기
        yield return null;
        yield return new WaitForEndOfFrame();

        // 2. 저장된 데이터 확인 (튜토리얼을 이미 깼는지?)
        if (GameManager.Instance != null && GameManager.Instance.Data.CurrentData != null)
        {
            if (GameManager.Instance.Data.CurrentData.isTutorialClear)
            {
                // 이미 깼다면 아무것도 안 하고 종료 (자유 이동 가능)
                Debug.Log("[Prologue] 튜토리얼 완료 상태. 연출 스킵.");
                yield break;
            }
        }

        // 3. 안 깼다면 연출 시작

        // [중요] 연출 도중 F키 눌러서 대화 꼬이는 것 방지
        if (npcInteractable != null)
            npcInteractable.canInteract = false;

        StartCoroutine(PrologueSequence());
    }

    IEnumerator PrologueSequence()
    {
        // 플레이어 이동 멈춤
        if (playerMovement != null) playerMovement.enabled = false;

        yield return new WaitForSeconds(0.5f);

        // 카메라가 NPC 쪽으로 이동
        if (dialogueCamera != null)
            dialogueCamera.FocusOnce(npcTransform, 0.8f);

        yield return new WaitForSeconds(0.8f); // 이동 시간 대기

        // 대화 카메라 모드로 전환
        if (dialogueCamera != null)
            dialogueCamera.StartDialogueCamera(npcTransform);

        // 대화 시작 (ID: DLG_1001)
        // autoStart: true로 설정하여 시작하자마자 넘기기 방지
        DialogueManager.Instance.StartDialogue(
            "DLG_1001",
            OnDialogueEnd,
            npcTransform,
            false
        );
    }

    // 대화가 끝났을 때 실행되는 함수
    private void OnDialogueEnd()
    {
        Debug.Log("[Prologue] 대화 종료. 상태 복구 및 저장.");

        // ▼▼▼ [F키 먹통 해결] NPC 상호작용 다시 켜주기 ▼▼▼
        if (npcInteractable != null)
        {
            npcInteractable.canInteract = true;
        }

        // 플레이어 움직임 다시 켜기
        if (playerMovement != null) playerMovement.enabled = true;

        // 튜토리얼 완료 저장 (GameManager 호출)
        if (GameManager.Instance != null)
        {
            GameManager.Instance.CompleteTutorial();
        }

        // 다음 튜토리얼(이동 가이드) 시작
        if (TutorialManager.Instance != null)
        {
            TutorialManager.Instance.StartMoveTutorial();
        }
        else
        {
            Debug.LogError("TutorialManager Instance is NULL");
        }
    }
}