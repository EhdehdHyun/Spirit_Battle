using UnityEngine;
using System.Collections;

public class BoxTutorialTrigger : MonoBehaviour
{
    [SerializeField] private DialogueCameraController cameraController;
    [SerializeField] private Transform boxCameraPoint;

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

        cameraController.FocusOnce(boxCameraPoint, 1.3f);
        yield return new WaitForSeconds(2f);
    }
}