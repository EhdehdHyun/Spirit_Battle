using UnityEngine;
using System.Collections;
public class BoxTutorialTrigger : MonoBehaviour
{
    [SerializeField] private DialogueCameraController cameraController;
    [SerializeField] private Transform boxCameraPoint;
    [SerializeField] private MonoBehaviour playerMovement;

    private bool triggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (triggered) return;
        if (!other.CompareTag("Player")) return;

        triggered = true;
        StartCoroutine(TutorialSequence());
    }
    private IEnumerator TutorialSequence()
    {
        if (cameraController == null || boxCameraPoint == null)
        {
            Debug.LogError("[BoxTutorialTrigger] CameraController or CameraPoint is NULL");
            yield break;
        }

        // 플레이어 이동 잠금
        if (playerMovement != null)
            playerMovement.enabled = false;
        
        cameraController.FocusOnce(boxCameraPoint, 1.3f, 10f);
        yield return new WaitForSeconds(2f);
        
        //플레이어 이동 해제
        if (playerMovement != null)
            playerMovement.enabled = true;
    }
}