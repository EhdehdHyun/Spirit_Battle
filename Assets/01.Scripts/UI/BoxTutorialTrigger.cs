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
        //worldArrow.gameObject.SetActive(true);
        StartCoroutine(TutorialSequence());
    }

    private IEnumerator TutorialSequence()
    {
        if (boxInteract == null)
        {
            Debug.LogError("[BoxTutorialTrigger] boxInteract is NULL");
            yield break;
        }

        boxInteract.enabled = false;

        cameraController.FocusOnce(boxCameraPoint, 1.3f);
        yield return new WaitForSeconds(2f);

        boxInteract.enabled = true;
    }

}