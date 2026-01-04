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

    // ===============================
    // 대화 시작 (NPC 바라보기)
    // ===============================

    public void StartDialogueCamera(Transform npc)
    {
        if (camRoutine != null)
            StopCoroutine(camRoutine);

        // 기준값 저장
        originalRotation = mainCamera.transform.rotation;
        originalFOV = mainCamera.fieldOfView;

        if (thirdPersonCamera != null)
            thirdPersonCamera.enabled = false;

        currentNPCTarget = npc;
        camRoutine = StartCoroutine(DialogueCamRoutine());
    }

    // DialogueManager에서 기다릴 수 있는 버전
    public IEnumerator StartDialogueCameraRoutine(Transform npc)
    {
        StartDialogueCamera(npc);
        yield return camRoutine; //  NPC 바라보기 끝까지 대기
    }

    // ===============================
    // 대화 종료 (플레이어 쪽 복귀)
    // ===============================

    public IEnumerator EndDialogueCameraRoutine()
    {
        if (camRoutine != null)
            StopCoroutine(camRoutine);

        camRoutine = StartCoroutine(ResetCamRoutine());
        yield return camRoutine; // 복귀 끝까지 대기
    }

    // ===============================
    // 내부 코루틴들
    // ===============================

    private IEnumerator DialogueCamRoutine()
    {
        float t = 0f;

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

    private IEnumerator ResetCamRoutine()
    {
        float t = 0f;

        Quaternion startRot = mainCamera.transform.rotation;
        float startFOV = mainCamera.fieldOfView;

        while (t < 1f)
        {
            t += Time.deltaTime * zoomSpeed;

            mainCamera.transform.rotation =
                Quaternion.Slerp(startRot, originalRotation, t);

            mainCamera.fieldOfView =
                Mathf.Lerp(startFOV, originalFOV, t);

            yield return null;
        }

        mainCamera.transform.rotation = originalRotation;
        mainCamera.fieldOfView = originalFOV;

        if (thirdPersonCamera != null)
            thirdPersonCamera.enabled = true;
    }

    // ===============================
    // 단발성 포커스 연출 
    // ===============================

    public void FocusOnce(Transform target, float focusTime = 1.2f, float customFOV = -1f)
    {
        if (camRoutine != null)
            StopCoroutine(camRoutine);

        camRoutine = StartCoroutine(FocusOnceRoutine(target, focusTime, customFOV));
    }

    private IEnumerator FocusOnceRoutine(Transform target, float holdTime, float customFOV)
    {
        if (thirdPersonCamera != null)
            thirdPersonCamera.enabled = false;

        originalRotation = mainCamera.transform.rotation;
        originalFOV = mainCamera.fieldOfView;

        float targetFOV = (customFOV > 0f) ? customFOV : zoomInFOV;

        Quaternion startRot = mainCamera.transform.rotation;
        float startFOV = mainCamera.fieldOfView;

        Vector3 targetPos = target.position;
        targetPos.y += 0.5f;

        Quaternion targetRot =
            Quaternion.LookRotation((targetPos - mainCamera.transform.position).normalized);

        float t = 0f;
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
        yield return ResetCamRoutine();
    }
}
