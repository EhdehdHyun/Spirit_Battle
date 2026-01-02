using UnityEngine;

public class AttackTutorialTrigger : MonoBehaviour
{
    private bool triggered;

    private void OnTriggerEnter(Collider other)
    {
        if (triggered) return;
        if (!other.CompareTag("Player")) return;

        triggered = true;

        // UI 공격 튜토리얼 시작
        TutorialManager.Instance.StartAttackTutorial();
    }
}