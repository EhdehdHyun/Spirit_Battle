using UnityEngine;
using TMPro;

public class JumpTutorialTrigger : MonoBehaviour
{
    [SerializeField] private GameObject jumpGuideText;

    private bool isActive = false;
    private bool completed = false;

    private void OnTriggerEnter(Collider other)
    {
        if (completed) return;
        if (!other.CompareTag("Player")) return;

        isActive = true;
        jumpGuideText.SetActive(true);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        isActive = false;
        jumpGuideText.SetActive(false);
    }

    private void Update()
    {
        if (!isActive || completed) return;

        if (Input.GetKeyDown(KeyCode.Space))
        {
            CompleteTutorial();
        }
    }

    private void CompleteTutorial()
    {
        completed = true;
        jumpGuideText.SetActive(false);

        // 필요하면 여기서 다음 튜토리얼 호출 가능
        // TutorialManager.Instance?.NextSomething();

        Destroy(gameObject); // 한 번만 쓰고 제거
    }
}