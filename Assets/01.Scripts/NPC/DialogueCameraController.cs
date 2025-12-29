using UnityEngine;
using System.Collections;

public class DialogueCameraController : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Camera mainCamera;

    [Header("Zoom")]
    [SerializeField] private float zoomInFOV = 45f;
    [SerializeField] private float zoomSpeed = 5f;

    [Header("Look")]
    [SerializeField] private float lookSpeed = 5f;
    
    [Header("Lock Targets")]
    [SerializeField] private ThirdPersonCamera thirdPersonCamera;
    
    private float originalFOV;
    private Quaternion originalRotation;
    private Transform currentNPCTarget;

    private Coroutine camRoutine;

    private void Awake()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        originalFOV = mainCamera.fieldOfView;
        originalRotation = mainCamera.transform.rotation;
    }

    public void StartDialogueCamera(Transform npc)
    {
        if (camRoutine != null)
            StopCoroutine(camRoutine);
        //대화 시작 직전에 기준값 저장
        originalRotation = mainCamera.transform.rotation;
        originalFOV = mainCamera.fieldOfView;

        if (thirdPersonCamera != null)
            thirdPersonCamera.enabled = false;

        currentNPCTarget = npc;
        camRoutine = StartCoroutine(DialogueCamRoutine());
    }

    public void EndDialogueCamera()
    {
        if (camRoutine != null)
            StopCoroutine(camRoutine);

        camRoutine = StartCoroutine(ResetCamRoutine());
    }

    IEnumerator DialogueCamRoutine()
    {
        float t = 0f;

        Vector3 startPos = mainCamera.transform.position;
        Quaternion startRot = mainCamera.transform.rotation;
        float startFOV = mainCamera.fieldOfView;

        Vector3 targetPos = currentNPCTarget.position;
        targetPos.y = mainCamera.transform.position.y;

        Quaternion targetRot =
            Quaternion.LookRotation((targetPos - mainCamera.transform.position).normalized);

        while (t < 1f)
        {
            t += Time.deltaTime * lookSpeed;

            mainCamera.transform.rotation =
                Quaternion.Slerp(startRot, targetRot, t);

            mainCamera.fieldOfView =
                Mathf.Lerp(startFOV, zoomInFOV, t);

            yield return null;
        }
        mainCamera.transform.rotation = targetRot;
        mainCamera.fieldOfView = zoomInFOV;
    }


    IEnumerator ResetCamRoutine()
    {
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * zoomSpeed;

            mainCamera.transform.rotation =
                Quaternion.Slerp(mainCamera.transform.rotation, originalRotation, t);

            mainCamera.fieldOfView =
                Mathf.Lerp(mainCamera.fieldOfView, originalFOV, t);

            yield return null;
        }
        if (thirdPersonCamera != null)
            thirdPersonCamera.enabled = true;
    }
    public void FocusOnce(Transform target, float focusTime = 1.2f, float customFOV = -1f)
    {
        if (camRoutine != null)
            StopCoroutine(camRoutine);

        camRoutine = StartCoroutine(FocusOnceRoutine(target, focusTime, customFOV));
    }

    IEnumerator FocusOnceRoutine(Transform target, float holdTime, float customFOV)
    {
        if (thirdPersonCamera != null)
            thirdPersonCamera.enabled = false;

        originalRotation = mainCamera.transform.rotation;
        originalFOV = mainCamera.fieldOfView;

        float targetFOV = (customFOV > 0f) ? customFOV : zoomInFOV;

        float t = 0f;

        Quaternion startRot = mainCamera.transform.rotation;
        float startFOV = mainCamera.fieldOfView;

        Vector3 targetPos = target.position;
        targetPos.y += 0.5f;
        Quaternion targetRot =
            Quaternion.LookRotation((targetPos - mainCamera.transform.position).normalized);

        while (t < 1f)
        {
            t += Time.deltaTime * lookSpeed;

            mainCamera.transform.rotation =
                Quaternion.Slerp(startRot, targetRot, t);

            mainCamera.fieldOfView =
                Mathf.Lerp(startFOV, targetFOV, t);

            yield return null;
        }

        yield return new WaitForSeconds(holdTime);
        yield return StartCoroutine(ResetCamRoutine());
    }
}
