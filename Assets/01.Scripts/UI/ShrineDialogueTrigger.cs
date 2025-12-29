using UnityEngine;
using System.Collections;

public class ShrineDialogueTrigger : MonoBehaviour
{
    [SerializeField] private string dialogueID = "DLG_SHRINE_01";
    [SerializeField] private Transform lookTarget; // 성소 Transform
    [SerializeField] private float autoCloseTime = 2.5f;

    private bool triggered;

    private void OnTriggerEnter(Collider other)
    {
        if (triggered) return;
        if (!other.CompareTag("Player")) return;

        triggered = true;
        
        TutorialManager.Instance.EndTutorialUI();

        DialogueManager.Instance.StartDialogue(
            dialogueID,
            OnDialogueEnd,
            lookTarget
        );

        StartCoroutine(AutoEnd());
    }

    IEnumerator AutoEnd()
    {
        yield return new WaitForSeconds(autoCloseTime);
        DialogueManager.Instance.Next(); // NextID 없으면 EndDialogue로 떨어짐
    }

    void OnDialogueEnd()
    {
        // 여기서 몬스터 전투 활성화 / 다음 단계 진입 가능
    }
}