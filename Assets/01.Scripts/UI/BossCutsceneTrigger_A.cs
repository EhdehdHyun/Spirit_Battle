using System.Collections;
using UnityEngine;

public class BossCutsceneTrigger_A : MonoBehaviour
{
    [Header("컷씬 시작 딜레이(초)")]
    [SerializeField] private float cutsceneDelay = 5f;

    [Header("카메라")]
    [SerializeField] private Camera mainCamera;         // 비워두면 Camera.main
    [SerializeField] private Camera cutsceneCamera;     // CutsceneCamera

    [Header("컷씬 경로(순서대로) - 최소 2개")]
    [SerializeField] private Transform[] points;

    [Header("바라볼 대상(선택)")]
    [SerializeField] private Transform lookTarget;      // 보스/오벨리스크 등

    [Header("구간 시간( points.Length-1 개 )")]
    [Tooltip("각 구간 이동 시간(초). points가 5개면 4개 필요")]
    [SerializeField] private float[] moveDurations;

    [Tooltip("각 포인트 도착 후 정지 시간(초). points가 5개면 5개까지 넣어도 됨(없으면 0)")]
    [SerializeField] private float[] holdDurations;

    [SerializeField] private float defaultMoveDuration = 1.5f;

    [Header("옵션")]
    [SerializeField] private bool freezeTimeScale = true;
    [SerializeField] private MonoBehaviour[] disableWhileCutscene;
    [SerializeField] private KeyCode skipKey = KeyCode.Escape;

    [Header("재생 제한")]
    [SerializeField] private bool playOnlyOnce = true;

    private bool played;
    private float prevTimeScale = 1f;
    private Coroutine co;

    private void Reset()
    {
        var col = GetComponent<Collider>();
        if (col) col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        if (playOnlyOnce && played) return;
        if (co != null) return; // 중복 코루틴 방지

        co = StartCoroutine(CoDelayThenPlay());
    }

    private IEnumerator CoDelayThenPlay()
    {
        played = true;

        if (cutsceneDelay > 0f)
            yield return new WaitForSeconds(cutsceneDelay);

        yield return CoPlay();

        co = null;
    }

    private IEnumerator CoPlay()
    {
        if (!mainCamera) mainCamera = Camera.main;

        if (!mainCamera || !cutsceneCamera || points == null || points.Length < 2)
        {
            Debug.LogError("[BossCutsceneTrigger_A] 세팅 누락(mainCamera/cutsceneCamera/points>=2)");
            yield break;
        }

        // 입력 잠금
        if (disableWhileCutscene != null)
        {
            foreach (var s in disableWhileCutscene)
                if (s) s.enabled = false;
        }

        // 시간 정지(선택)
        if (freezeTimeScale)
        {
            prevTimeScale = Time.timeScale;
            Time.timeScale = 0f;
        }

        // 카메라 전환
        mainCamera.gameObject.SetActive(false);
        cutsceneCamera.gameObject.SetActive(true);

        // 시작 포인트로
        cutsceneCamera.transform.position = points[0].position;
        cutsceneCamera.transform.rotation = points[0].rotation;

        // 시작 포인트 홀드
        yield return HoldAtPoint(0);

        // 구간 이동 반복
        for (int seg = 0; seg < points.Length - 1; seg++)
        {
            float dur = GetMoveDuration(seg);
            Transform a = points[seg];
            Transform b = points[seg + 1];

            float t = 0f;
            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                float lerp = Mathf.Clamp01(t / dur);

                cutsceneCamera.transform.position = Vector3.Lerp(a.position, b.position, lerp);

                if (lookTarget)
                {
                    Vector3 dir = (lookTarget.position - cutsceneCamera.transform.position);
                    if (dir.sqrMagnitude > 0.0001f)
                    {
                        Quaternion lookRot = Quaternion.LookRotation(dir.normalized, Vector3.up);
                        cutsceneCamera.transform.rotation =
                            Quaternion.Slerp(cutsceneCamera.transform.rotation, lookRot, 0.25f);
                    }
                }
                else
                {
                    cutsceneCamera.transform.rotation = Quaternion.Slerp(a.rotation, b.rotation, lerp);
                }

                // 스킵
                if (Input.GetKeyDown(skipKey))
                {
                    seg = points.Length; // 바깥 루프 종료 유도
                    break;
                }

                yield return null;
            }

            // 도착 포인트 홀드 (seg+1)
            if (seg < points.Length) // 안전
                yield return HoldAtPoint(seg + 1);
        }

        // 원복
        cutsceneCamera.gameObject.SetActive(false);
        mainCamera.gameObject.SetActive(true);

        if (freezeTimeScale)
            Time.timeScale = prevTimeScale;

        if (disableWhileCutscene != null)
        {
            foreach (var s in disableWhileCutscene)
                if (s) s.enabled = true;
        }
    }

    private float GetMoveDuration(int segmentIndex)
    {
        if (moveDurations != null && segmentIndex >= 0 && segmentIndex < moveDurations.Length)
        {
            float v = moveDurations[segmentIndex];
            if (v > 0.01f) return v;
        }
        return Mathf.Max(0.01f, defaultMoveDuration);
    }

    private IEnumerator HoldAtPoint(int pointIndex)
    {
        float hold = 0f;
        if (holdDurations != null && pointIndex >= 0 && pointIndex < holdDurations.Length)
            hold = Mathf.Max(0f, holdDurations[pointIndex]);

        if (hold <= 0f) yield break;

        float t = 0f;
        while (t < hold)
        {
            t += Time.unscaledDeltaTime;

            if (Input.GetKeyDown(skipKey))
                yield break;

            yield return null;
        }
    }
}
