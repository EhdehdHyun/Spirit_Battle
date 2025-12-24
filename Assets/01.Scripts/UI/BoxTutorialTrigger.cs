using UnityEngine;
using System.Collections;

public class BoxTutorialTrigger : MonoBehaviour
{
    [SerializeField] private DialogueCameraController cameraController;
    [SerializeField] private Transform boxCameraPoint;
    [SerializeField] private BoxInteract boxInteract;
   // [SerializeField] private MonoBehaviour playerMovement;

    [Header("Guide Text")]
    [SerializeField] private GameObject guideText;

    private bool triggered = false;

    private void Awake()
    {
        if (guideText != null)
            guideText.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (triggered) return;
        if (!other.CompareTag("Player")) return;

        triggered = true;
        StartCoroutine(TutorialSequence());
    }

    private IEnumerator TutorialSequence()
    {
        //  플레이어 입력 잠금
        //if (playerMovement != null)
            //playerMovement.enabled = false;
        //  상호작용 잠금
        boxInteract.canInteract = false;

        //  카메라 포커스
        cameraController.FocusOnce(boxCameraPoint, 1.3f);

        // 카메라 연출 대기
        yield return new WaitForSeconds(2f);

        // 가이드 텍스트 표시
        if (guideText != null)
            guideText.SetActive(true);

        //  상호작용 해금
        boxInteract.canInteract = true;
    }
}