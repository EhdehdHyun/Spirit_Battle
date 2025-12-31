using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class BossCutsceneTrigger_A : MonoBehaviour
{
    [Header("컷씬 시작 딜레이(초) - 보스 생성 타이밍 맞출 때 사용")]
    [SerializeField] private float cutsceneDelay = 0f; // unscaled 기준

    [Header("컷씬 동안 Time.timeScale (0~1)")]
    [Range(0f, 1f)]
    [SerializeField] private float cutsceneTimeScale = 0.4f;

    [Header("카메라")]
    [SerializeField] private Camera mainCamera;         // 비워두면 Camera.main
    [SerializeField] private Camera cutsceneCamera;     // 컷씬용 Camera

    [Header("컷씬 경로(순서대로) - 최소 2개")]
    [SerializeField] private Transform[] points;

    [Header("바라볼 대상(선택)")]
    [SerializeField] private Transform lookTarget;

    [Header("구간 시간( points.Length-1 개 )")]
    [Tooltip("각 구간 이동 시간(초). points가 5개면 4개 필요")]
    [SerializeField] private float[] moveDurations;

    [Tooltip("각 포인트 도착 후 정지 시간(초). points가 5개면 5개까지 넣어도 됨(없으면 0)")]
    [SerializeField] private float[] holdDurations;

    [SerializeField] private float defaultMoveDuration = 1.5f;

    [Header("옵션 - 컷씬 동안 비활성화할 스크립트(이동/입력 등)")]
    [SerializeField] private MonoBehaviour[] disableWhileCutscene;

    [Header("옵션 - 컷씬 동안 비활성화할 UI 오브젝트")]
    [SerializeField] private GameObject[] uiToDisableWhileCutscene;

    [Header("스킵 키")]
    [SerializeField] private KeyCode skipKey = KeyCode.Escape;

    [Header("재생 제한")]
    [SerializeField] private bool playOnlyOnce = true;

    // ===== TestBoss2 연출 =====
    [Header("컷씬용 보스(TestBoss2)")]
    [SerializeField] private GameObject testBoss2Root;     // 컷씬용 보스 루트(평소 비활성화)
    [SerializeField] private Animator boss2Animator;       // 비워두면 testBoss2Root에서 자동 탐색
    [SerializeField] private string walkStateName = "Walk";
    [SerializeField] private float walkDuration = 3f;
    [SerializeField] private string phase2StateName = "3phase_2";
    [SerializeField] private float phase2Duration = 2f;
    [SerializeField] private bool disableBoss2OnEnd = true;

    [Header("실제 전투 보스(선택) - 혹시 자동 생성이 꼬일 때 강제 활성화용")]
    [SerializeField] private GameObject realBossRoot;
    [SerializeField] private bool forceActivateRealBossAtEnd = false;

    // ===== Letterbox =====
    [Header("Letterbox(검은 바) - 컷씬에서만 보이기")]
    [SerializeField] private GameObject letterboxRoot;
    [SerializeField] private RectTransform letterboxTop;
    [SerializeField] private RectTransform letterboxBottom;
    [SerializeField] private float letterboxHeight = 160f;
    [SerializeField] private float letterboxAnimTime = 0.25f;
    [SerializeField] private float letterboxPreRoll = 0.2f;
    [SerializeField] private float letterboxPostRoll = 0.2f;

    private bool played;
    private bool playing;
    private bool skipRequested;
    private float prevTimeScale = 1f;

    private void Reset()
    {
        var col = GetComponent<Collider>();
        if (col) col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (playing) return;
        if (playOnlyOnce && played) return;

        played = true;
        StartCoroutine(CoPlay());
    }

    private IEnumerator CoPlay()
    {
        playing = true;
        skipRequested = false;

        // 딜레이(보스 5초 생성에 맞추려면 여기 숫자 조절)
        if (cutsceneDelay > 0f)
            yield return WaitUnscaled(cutsceneDelay);

        if (!mainCamera) mainCamera = Camera.main;

        if (!mainCamera || !cutsceneCamera || points == null || points.Length < 2)
        {
            Debug.LogError("[BossCutsceneTrigger_A] 세팅 누락(mainCamera/cutsceneCamera/points>=2)");
            playing = false;
            yield break;
        }

        // 컷씬 시작 전: 레터박스 루트 켜기(컷씬에서만 보이게)
        if (letterboxRoot) letterboxRoot.SetActive(true);

        // 입력 잠금
        SetScriptsEnabled(disableWhileCutscene, false);

        // UI 끄기(원하는 UI만 여기 넣기)
        SetObjectsActive(uiToDisableWhileCutscene, false);

        // 타임스케일 (0~1)
        prevTimeScale = Time.timeScale;
        Time.timeScale = Mathf.Clamp01(cutsceneTimeScale);

        // 카메라 전환
        mainCamera.gameObject.SetActive(false);
        cutsceneCamera.gameObject.SetActive(true);

        // 시작 포인트 적용
        cutsceneCamera.transform.position = points[0].position;
        cutsceneCamera.transform.rotation = points[0].rotation;

        // 레터박스 IN
        if (HasLetterbox())
        {
            InitLetterboxZero();
            yield return CoLetterbox(true);
            if (letterboxPreRoll > 0f) yield return WaitUnscaled(letterboxPreRoll);
        }

        // TestBoss2 연출 시작(동시에)
        if (testBoss2Root)
            StartCoroutine(CoPlayBoss2Sequence());

        // 시작 포인트 홀드
        yield return HoldAtPoint(0);
        if (skipRequested) { yield return CoFinish(); yield break; }

        // 구간 이동
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
                    skipRequested = true;
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

            if (skipRequested) break;

            // 도착 포인트 홀드
            yield return HoldAtPoint(seg + 1);
            if (skipRequested) break;
        }

        yield return CoFinish();
    }

    private IEnumerator CoFinish()
    {
        // 레터박스 OUT
        if (HasLetterbox())
        {
            if (letterboxPostRoll > 0f) yield return WaitUnscaled(letterboxPostRoll);
            yield return CoLetterbox(false);
        }

        // 레터박스 루트 끄기(컷씬에서만 보이게)
        if (letterboxRoot) letterboxRoot.SetActive(false);

        // 카메라 원복
        if (cutsceneCamera) cutsceneCamera.gameObject.SetActive(false);
        if (mainCamera) mainCamera.gameObject.SetActive(true);

        // 타임스케일 원복
        Time.timeScale = prevTimeScale;

        // 입력/스크립트 원복
        SetScriptsEnabled(disableWhileCutscene, true);

        // UI 원복
        SetObjectsActive(uiToDisableWhileCutscene, true);

        // 필요하면 보스 강제 활성화(자동 5초 생성이 있으면 보통 OFF로 둬도 됨)
        if (forceActivateRealBossAtEnd && realBossRoot)
            realBossRoot.SetActive(true);

        playing = false;
    }

    // ===== Boss2 연출 =====
    private IEnumerator CoPlayBoss2Sequence()
    {
        testBoss2Root.SetActive(true);

        if (!boss2Animator)
            boss2Animator = testBoss2Root.GetComponentInChildren<Animator>(true);

        if (!boss2Animator)
        {
            Debug.LogWarning("[BossCutsceneTrigger_A] Boss2 Animator가 없습니다.");
            yield break;
        }

        // Walk 3초
        boss2Animator.Play(walkStateName, 0, 0f);
        yield return WaitUnscaled(walkDuration);

        // 3phase_2 2초
        boss2Animator.Play(phase2StateName, 0, 0f);
        yield return WaitUnscaled(phase2Duration);

        if (disableBoss2OnEnd && testBoss2Root)
            testBoss2Root.SetActive(false);
    }

    // ===== Utils =====
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
            {
                skipRequested = true;
                yield break;
            }

            t += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    private IEnumerator WaitUnscaled(float seconds)
    {
        if (seconds <= 0f) yield break;

        float t = 0f;
        while (t < seconds)
        {
            if (Input.GetKeyDown(skipKey))
            {
                skipRequested = true;
                yield break;
            }

            t += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    private void SetScriptsEnabled(MonoBehaviour[] arr, bool enabled)
    {
        if (arr == null) return;
        foreach (var s in arr)
            if (s) s.enabled = enabled;
    }

    private void SetObjectsActive(GameObject[] arr, bool active)
    {
        if (arr == null) return;
        foreach (var go in arr)
            if (go) go.SetActive(active);
    }

    // ===== Letterbox =====
    private bool HasLetterbox()
    {
        return letterboxTop && letterboxBottom;
    }

    private void InitLetterboxZero()
    {
        // 시작은 높이 0 (안 보이게)
        var topSize = letterboxTop.sizeDelta;
        topSize.y = 0f;
        letterboxTop.sizeDelta = topSize;

        var bottomSize = letterboxBottom.sizeDelta;
        bottomSize.y = 0f;
        letterboxBottom.sizeDelta = bottomSize;

        // 혹시 색이 투명일 수 있으니 Image 있으면 검정으로 보정
        var topImg = letterboxTop.GetComponent<Image>();
        if (topImg) topImg.color = new Color(0, 0, 0, 1);

        var botImg = letterboxBottom.GetComponent<Image>();
        if (botImg) botImg.color = new Color(0, 0, 0, 1);
    }

    private IEnumerator CoLetterbox(bool show)
    {
        float from = show ? 0f : letterboxHeight;
        float to = show ? letterboxHeight : 0f;

        float t = 0f;
        float dur = Mathf.Max(0.01f, letterboxAnimTime);

        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float lerp = Mathf.Clamp01(t / dur);
            float h = Mathf.Lerp(from, to, lerp);

            var topSize = letterboxTop.sizeDelta;
            topSize.y = h;
            letterboxTop.sizeDelta = topSize;

            var bottomSize = letterboxBottom.sizeDelta;
            bottomSize.y = h;
            letterboxBottom.sizeDelta = bottomSize;

            yield return null;
        }
    }
}
