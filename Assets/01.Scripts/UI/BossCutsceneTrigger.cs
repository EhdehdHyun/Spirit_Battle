using System.Collections;
using UnityEngine;

public class BossCutsceneTrigger : MonoBehaviour
{
    [Header("필수")]
    [SerializeField] private Camera mainCamera;       // 기존 플레이 카메라
    [SerializeField] private Camera cutsceneCamera;   // CutsceneCamera
    [SerializeField] private Transform lookTarget;    // 보스/오벨리스크 등 바라볼 대상

    [Header("컷씬 카메라 시작/끝 포즈")]
    [SerializeField] private Transform camStart;
    [SerializeField] private Transform camEnd;

    [Header("연출")]
    [SerializeField] private float duration = 2.0f;   // 이동 시간
    [SerializeField] private bool freezeTimeScale = true;
    [SerializeField] private MonoBehaviour[] disableWhileCutscene; // 플레이어 입력/전투 스크립트 등

    private bool played;
    private float prevTimeScale = 1f;

    private void Reset()
    {
        // 트리거로 쓰기 편하게
        var col = GetComponent<Collider>();
        if (col) col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (played) return;
        if (!other.CompareTag("Player")) return;

        played = true;
        StartCoroutine(CoPlay());
    }

    private IEnumerator CoPlay()
    {
        if (!mainCamera) mainCamera = Camera.main;
        if (!mainCamera || !cutsceneCamera || !camStart || !camEnd)
        {
            Debug.LogError("[BossCutsceneTrigger] camera refs missing");
            yield break;
        }

        // 1) 입력 잠금 + (선택) 시간정지
        foreach (var s in disableWhileCutscene)
            if (s) s.enabled = false;

        if (freezeTimeScale)
        {
            prevTimeScale = Time.timeScale;
            Time.timeScale = 0f;
        }

        // 2) 카메라 전환
        mainCamera.gameObject.SetActive(false);
        cutsceneCamera.gameObject.SetActive(true);

        // 3) 컷씬 카메라 포즈 세팅
        cutsceneCamera.transform.position = camStart.position;
        cutsceneCamera.transform.rotation = camStart.rotation;

        // TimeScale=0 이어도 진행되게 UnscaledTime 사용
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float a = Mathf.Clamp01(t / duration);

            cutsceneCamera.transform.position = Vector3.Lerp(camStart.position, camEnd.position, a);

            if (lookTarget)
            {
                Vector3 dir = (lookTarget.position - cutsceneCamera.transform.position);
                if (dir.sqrMagnitude > 0.0001f)
                    cutsceneCamera.transform.rotation = Quaternion.Slerp(
                        camStart.rotation,
                        Quaternion.LookRotation(dir.normalized, Vector3.up),
                        a
                    );
            }
            else
            {
                cutsceneCamera.transform.rotation = Quaternion.Slerp(camStart.rotation, camEnd.rotation, a);
            }

            yield return null;
        }

        // 4) 복귀
        cutsceneCamera.gameObject.SetActive(false);
        mainCamera.gameObject.SetActive(true);

        if (freezeTimeScale)
            Time.timeScale = prevTimeScale;

        foreach (var s in disableWhileCutscene)
            if (s) s.enabled = true;
    }
}
