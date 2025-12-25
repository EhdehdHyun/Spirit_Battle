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
        Vector3 startPos = mainCamera.transform.position;

        while (true)
        {
            // ▶ NPC 기준 오프셋 (옆 + 얼굴 높이)
            Vector3 targetPos = currentNPCTarget.position;
            targetPos += currentNPCTarget.right * -0.8f;   // 좌우 구도
            targetPos += Vector3.up * 1.6f;               // 얼굴 높이

            // ▶ 회전
            Vector3 dir = (targetPos - mainCamera.transform.position).normalized;
            Quaternion lookRot = Quaternion.LookRotation(dir);

            mainCamera.transform.rotation =
                Quaternion.Slerp(
                    mainCamera.transform.rotation,
                    lookRot,
                    Time.deltaTime * lookSpeed
                );

            // ▶ 줌
            mainCamera.fieldOfView =
                Mathf.Lerp(
                    mainCamera.fieldOfView,
                    zoomInFOV,
                    Time.deltaTime * zoomSpeed
                );

            // ▶ 거리 연출 (앞으로 살짝만, 누적 ❌)
            mainCamera.transform.position =
                Vector3.Lerp(
                    mainCamera.transform.position,
                    startPos + mainCamera.transform.forward * 0.4f,
                    Time.deltaTime * 0.5f
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
