using System.Collections;
using UnityEngine;

public class BossCutsceneTrigger_A : MonoBehaviour
{
    [Header("컷씬 시작 딜레이(초) - 보스 생성 5초 맞추기")]
    [SerializeField] private float cutsceneDelay = 0f;

    [Header("컷씬 동안 Time.timeScale (0~1)")]
    [Range(0f, 1f)]
    [SerializeField] private float cutsceneTimeScale = 0f;   // 0이면 정지, 0.4면 슬로우

    [Header("카메라")]
    [SerializeField] private Camera mainCamera;              // 비워두면 Camera.main
    [SerializeField] private Camera cutsceneCamera;          // BossCutSceneCamera

    [Header("컷씬 경로(순서대로) - 최소 2개")]
    [SerializeField] private Transform[] points;

    [Header("바라볼 대상(선택)")]
    [SerializeField] private Transform lookTarget;           // 보스/오벨리스크 등

    [Header("구간 시간( points.Length-1 개 )")]
    [Tooltip("각 구간 이동 시간(초). points가 4개면 3개 필요")]
    [SerializeField] private float[] moveDurations;

    [Tooltip("각 포인트 도착 후 정지 시간(초). points가 4개면 4개까지 넣어도 됨(없으면 0)")]
    [SerializeField] private float[] holdDurations;

    [SerializeField] private float defaultMoveDuration = 1.5f;

    [Header("옵션 - 컷씬 동안 비활성화할 스크립트들(이동/입력 등)")]
    [SerializeField] private MonoBehaviour[] disableWhileCutscene;

    [Header("옵션 - 컷씬 동안 비활성화할 UI 오브젝트들")]
    [Tooltip("예: 인게임 HUD, 미니맵, 퀘스트 UI 등 (Canvas/Panel 아무거나 가능)")]
    [SerializeField] private GameObject[] uiToDisableWhileCutscene;

    [SerializeField] private KeyCode skipKey = KeyCode.Escape;

    [Header("재생 제한")]
    [SerializeField] private bool playOnlyOnce = true;

    [Header("Letterbox UI (위/아래 검은 바) - 컷씬에서만 켜짐")]
    [Tooltip("Canvas 아래에 있는 LetterBar 루트(부모 오브젝트)를 넣어주세요.")]
    [SerializeField] private GameObject letterboxRoot;        // LetterBar
    [SerializeField] private RectTransform letterboxTop;      // 선택(애니메이션 안 쓰면 없어도 됨)
    [SerializeField] private RectTransform letterboxBottom;   // 선택(애니메이션 안 쓰면 없어도 됨)
    [SerializeField] private float letterboxHeight = 160f;
    [SerializeField] private float letterboxAnimTime = 0.25f;
    [SerializeField] private float letterboxPreRoll = 0.2f;
    [SerializeField] private float letterboxPostRoll = 0.2f;

    private bool played;
    private bool isPlaying;
    private float prevTimeScale = 1f;

    // UI 원복용(컷씬 전에 켜져있던 애만 다시 켜기)
    private bool[] uiPrevActive;

    private float topOnY, topOffY;
    private float bottomOnY, bottomOffY;

    private void Reset()
    {
        var col = GetComponent<Collider>();
        if (col) col.isTrigger = true;
    }

    private void Awake()
    {
        CacheLetterboxPositions();
        if (letterboxRoot) letterboxRoot.SetActive(false);

        // UI 이전 상태 배열 준비
        if (uiToDisableWhileCutscene != null && uiToDisableWhileCutscene.Length > 0)
            uiPrevActive = new bool[uiToDisableWhileCutscene.Length];
    }

    private void CacheLetterboxPositions()
    {
        if (letterboxTop)
        {
            topOnY = 0f;
            topOffY = +letterboxHeight;
        }

        if (letterboxBottom)
        {
            bottomOnY = 0f;
            bottomOffY = -letterboxHeight;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isPlaying) return;
        if (playOnlyOnce && played) return;
        if (!other.CompareTag("Player")) return;

        played = true;
        StartCoroutine(CoPlay());
    }

    private IEnumerator CoPlay()
    {
        isPlaying = true;

        if (!mainCamera) mainCamera = Camera.main;

        if (!mainCamera || !cutsceneCamera || points == null || points.Length < 2)
        {
            Debug.LogError("[BossCutsceneTrigger_A] 세팅 누락(mainCamera/cutsceneCamera/points>=2)");
            isPlaying = false;
            yield break;
        }

        // 0) 딜레이
        if (cutsceneDelay > 0f)
        {
            float t = 0f;
            while (t < cutsceneDelay)
            {
                t += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        // 1) 입력/이동 스크립트 잠금
        SetScriptsEnabled(disableWhileCutscene, false);

        // 2) UI 잠금 (이전 상태 저장 후 끄기)
        CacheAndSetUI(false);

        // 3) TimeScale 저장 후 적용
        prevTimeScale = Time.timeScale;
        Time.timeScale = Mathf.Clamp01(cutsceneTimeScale);

        // 4) Letterbox 켜기
        yield return ShowLetterbox(true);

        // 5) 카메라 전환
        mainCamera.gameObject.SetActive(false);
        cutsceneCamera.gameObject.SetActive(true);

        // 6) 시작 포인트 스냅
        cutsceneCamera.transform.position = points[0].position;
        cutsceneCamera.transform.rotation = points[0].rotation;

        yield return HoldAtPoint(0);

        bool skipped = false;

        for (int seg = 0; seg < points.Length - 1; seg++)
        {
            float dur = GetMoveDuration(seg);
            Transform a = points[seg];
            Transform b = points[seg + 1];

            float t = 0f;
            while (t < dur)
            {
                if (Input.GetKeyDown(skipKey))
                {
                    skipped = true;
                    break;
                }

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

                yield return null;
            }

            if (skipped) break;

            yield return HoldAtPoint(seg + 1);

            if (Input.GetKeyDown(skipKey))
            {
                skipped = true;
                break;
            }
        }

        // 7) 원복 (공통 종료 처리)
        yield return CoEndCutscene();
    }

    private IEnumerator CoEndCutscene()
    {
        // 카메라 원복
        if (cutsceneCamera) AIMakeInactive(cutsceneCamera.gameObject);
        if (mainCamera) AIMakeActive(mainCamera.gameObject);

        // Letterbox 끄기(애니 포함)
        yield return ShowLetterbox(false);

        // TimeScale 원복
        Time.timeScale = prevTimeScale;

        // UI 원복 (컷씬 전에 켜져있던 것만)
        RestoreUI();

        // 스크립트 원복
        SetScriptsEnabled(disableWhileCutscene, true);

        isPlaying = false;
    }

    private void SetScriptsEnabled(MonoBehaviour[] arr, bool enabled)
    {
        if (arr == null) return;
        foreach (var s in arr)
            if (s) s.enabled = enabled;
    }

    private void CacheAndSetUI(bool active)
    {
        if (uiToDisableWhileCutscene == null || uiToDisableWhileCutscene.Length == 0)
            return;

        if (uiPrevActive == null || uiPrevActive.Length != uiToDisableWhileCutscene.Length)
            uiPrevActive = new bool[uiToDisableWhileCutscene.Length];

        for (int i = 0; i < uiToDisableWhileCutscene.Length; i++)
        {
            var go = uiToDisableWhileCutscene[i];
            if (!go) { uiPrevActive[i] = false; continue; }

            uiPrevActive[i] = go.activeSelf; // 이전 상태 저장
            go.SetActive(active);
        }
    }

    private void RestoreUI()
    {
        if (uiToDisableWhileCutscene == null || uiToDisableWhileCutscene.Length == 0)
            return;
        if (uiPrevActive == null) return;

        for (int i = 0; i < uiToDisableWhileCutscene.Length; i++)
        {
            var go = uiToDisableWhileCutscene[i];
            if (!go) continue;

            // 컷씬 전에 켜져있던 애만 다시 켜기
            go.SetActive(uiPrevActive[i]);
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
            if (Input.GetKeyDown(skipKey))
                yield break;

            t += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    // =========================
    // Letterbox
    // =========================
    private IEnumerator ShowLetterbox(bool show)
    {
        if (!letterboxRoot)
            yield break;

        if (show)
        {
            letterboxRoot.SetActive(true);

            if (letterboxTop && letterboxBottom && letterboxAnimTime > 0.01f)
            {
                SetBarY(letterboxTop, topOffY);
                SetBarY(letterboxBottom, bottomOffY);

                if (letterboxPreRoll > 0f)
                    yield return WaitUnscaled(letterboxPreRoll);

                yield return LerpBars(topOffY, topOnY, bottomOffY, bottomOnY, letterboxAnimTime);
            }
        }
        else
        {
            if (letterboxPostRoll > 0f)
                yield return WaitUnscaled(letterboxPostRoll);

            if (letterboxTop && letterboxBottom && letterboxAnimTime > 0.01f)
                yield return LerpBars(topOnY, topOffY, bottomOnY, bottomOffY, letterboxAnimTime);

            letterboxRoot.SetActive(false);
        }
    }

    private IEnumerator LerpBars(float topFrom, float topTo, float bottomFrom, float bottomTo, float dur)
    {
        float t = 0f;
        while (t < dur)
        {
            if (Input.GetKeyDown(skipKey))
                break;

            t += Time.unscaledDeltaTime;
            float a = Mathf.Clamp01(t / dur);

            if (letterboxTop) SetBarY(letterboxTop, Mathf.Lerp(topFrom, topTo, a));
            if (letterboxBottom) SetBarY(letterboxBottom, Mathf.Lerp(bottomFrom, bottomTo, a));

            yield return null;
        }

        if (letterboxTop) SetBarY(letterboxTop, topTo);
        if (letterboxBottom) SetBarY(letterboxBottom, bottomTo);
    }

    private void SetBarY(RectTransform rt, float y)
    {
        var ap = rt.anchoredPosition;
        ap.y = y;
        rt.anchoredPosition = ap;

        var size = rt.sizeDelta;
        size.y = letterboxHeight;
        rt.sizeDelta = size;
    }

    private IEnumerator WaitUnscaled(float seconds)
    {
        float t = 0f;
        while (t < seconds)
        {
            if (Input.GetKeyDown(skipKey))
                yield break;

            t += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    // 유틸(안전하게 활성/비활성)
    private void AIMakeActive(GameObject go)
    {
        if (go && !go.activeSelf) go.SetActive(true);
    }

    private void AIMakeInactive(GameObject go)
    {
        if (go && go.activeSelf) go.SetActive(false);
    }
}
