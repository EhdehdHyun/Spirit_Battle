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
        if (thirdPersonCamera != null)
            thirdPersonCamera.enabled = false; // 끄기

        originalRotation = mainCamera.transform.rotation;
        originalFOV = mainCamera.fieldOfView;
        
        currentNPCTarget = npc;

        if (camRoutine != null)
            StopCoroutine(camRoutine);

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
        while (true)
        {
            Vector3 targetPos = currentNPCTarget.position;
            targetPos.y = mainCamera.transform.position.y;

            Vector3 dir = (targetPos - mainCamera.transform.position).normalized;
            Quaternion lookRot = Quaternion.LookRotation(dir);

            mainCamera.transform.rotation =
                Quaternion.Slerp(
                    mainCamera.transform.rotation,
                    lookRot,
                    Time.deltaTime * lookSpeed
                );
            mainCamera.fieldOfView =
                Mathf.Lerp(
                    mainCamera.fieldOfView,
                    zoomInFOV,
                    Time.deltaTime * zoomSpeed
                );

            yield return null;
        }
    }

    IEnumerator ResetCamRoutine()
    {
        while (true)
        {
            mainCamera.transform.rotation =
                Quaternion.Slerp(mainCamera.transform.rotation, originalRotation, Time.deltaTime * lookSpeed);

            mainCamera.fieldOfView =
                Mathf.Lerp(mainCamera.fieldOfView, originalFOV, Time.deltaTime * zoomSpeed);

            if (Mathf.Abs(mainCamera.fieldOfView - originalFOV) < 0.1f)
                break;

            yield return null;
        }
        // 다시 켜기
        if (thirdPersonCamera != null)
            thirdPersonCamera.enabled = true;
    }
    public void FocusOnce(Transform target, float focusTime = 1.2f)
    {
        if (camRoutine != null)
            StopCoroutine(camRoutine);

        camRoutine = StartCoroutine(FocusOnceRoutine(target, focusTime));
    }
    IEnumerator FocusOnceRoutine(Transform target, float holdTime)
    {
        // 3인칭 카메라 잠금
        if (thirdPersonCamera != null)
            thirdPersonCamera.enabled = false;

        originalRotation = mainCamera.transform.rotation;
        originalFOV = mainCamera.fieldOfView;

        // ▶ 포커스
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * lookSpeed;

            Vector3 targetPos = target.position;
            targetPos.y = mainCamera.transform.position.y;

            Vector3 dir = (targetPos - mainCamera.transform.position).normalized;
            Quaternion lookRot = Quaternion.LookRotation(dir);

            mainCamera.transform.rotation =
                Quaternion.Slerp(mainCamera.transform.rotation, lookRot, Time.deltaTime * lookSpeed);

            mainCamera.fieldOfView =
                Mathf.Lerp(mainCamera.fieldOfView, zoomInFOV, Time.deltaTime * zoomSpeed);

            yield return null;
        }

        // ▶ 잠깐 유지
        yield return new WaitForSeconds(holdTime);

        // ▶ 원래 시점으로 복귀
        yield return StartCoroutine(ResetCamRoutine());
    }
}
