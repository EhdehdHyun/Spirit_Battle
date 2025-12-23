using UnityEngine;

public class AttackTutorialTrigger : MonoBehaviour
{
    [Header("Guide Text")]
    [SerializeField] private GameObject guideText;

    private bool isActive = false;
    private bool completed = false;

    private bool rightClickDone = false;
    private bool leftClickDone = false;

    private void Awake()
    {
        if (guideText != null)
            guideText.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (completed) return;
        if (!other.CompareTag("Player")) return;

        isActive = true;

        if (guideText != null)
            guideText.SetActive(true);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        isActive = false;

        // 영역 벗어나면 텍스트 숨김 (선택)
        if (!completed && guideText != null)
            guideText.SetActive(false);
    }

    private void Update()
    {
        if (!isActive || completed) return;

        // 오른쪽 클릭 (마우스 버튼 1)
        if (!rightClickDone && Input.GetMouseButtonDown(1))
        {
            rightClickDone = true;
            Debug.Log("Right Click detected (Weapon Draw)");
        }

        // 왼쪽 클릭 (마우스 버튼 0)
        if (!leftClickDone && Input.GetMouseButtonDown(0))
        {
            leftClickDone = true;
            Debug.Log("Left Click detected (Attack)");
        }

        // 둘 다 수행했으면 종료
        if (rightClickDone && leftClickDone)
        {
            CompleteTutorial();
        }
    }

    private void CompleteTutorial()
    {
        completed = true;
        isActive = false;

        if (guideText != null)
            guideText.SetActive(false);

        // 한 번만 실행
        Destroy(gameObject);
    }
}