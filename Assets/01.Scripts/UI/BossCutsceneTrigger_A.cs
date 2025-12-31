using System.Collections;
using UnityEngine;

public class BossCutsceneTrigger_A : MonoBehaviour
{
    [Header("카메라")]
    [SerializeField] private Camera mainCamera;         // 비워두면 Camera.main
    [SerializeField] private Camera cutsceneCamera;     // BossCutSceneCamera

    [Header("컷씬 딜레이(초) - 보스 생성 5초 맞추기")]
    [SerializeField] private float cutsceneDelay = 0f;

    [Header("컷씬 동안 Time.timeScale (0~1)")]
    [Range(0f, 1f)]
    [SerializeField] private float cutsceneTimeScale = 0f;

    [Header("컷씬 경로(순서대로) - 최소 2개")]
    [SerializeField] private Transform[] points;

    [Header("바라볼 대상(선택)")]
    [SerializeField] private Transform lookTarget;

    [Header("구간 이동 시간( points.Length-1 개 )")]
    [SerializeField] private float[] moveDurations;
    [SerializeField] private float defaultMoveDuration = 1.5f;

    [Header("포인트 홀드 시간(각 포인트)")]
    [SerializeField] private float[] holdDurations;

    [Header("옵션 - 컷씬 동안 비활성화할 스크립트(이동/입력 등)")]
    [SerializeField] private MonoBehaviour[] disableWhileCutscene;

    [Header("옵션 - 컷씬 동안 비활성화할 UI 오브젝트(패널/캔버스 등)")]
    [SerializeField] private GameObject[] uiToDisableWhileCutscene;

    [Header("스킵 키")]
    [SerializeField] private KeyCode skipKey = KeyCode.Escape;

    [Header("재생 제한")]
    [SerializeField] private bool playOnlyOnce = true;

    [Header("BGM (보스전 진입 시 재생)")]
    [SerializeField] private AudioSource bgmSource;     // 비워두면 자동 생성/탐색
    [SerializeField] private AudioClip bossBgmClip;     // 인스펙터에 넣기
    [Range(0f, 1f)]
    [SerializeField] private float bossBgmVolume = 1f;
    [SerializeField] private float bossBgmFadeIn = 0.5f;
    [SerializeField] private float bossBgmFadeOutPrev = 0.3f;

    [Header("보스전 BGM 종료 조건(필수)")]
    [Tooltip("실제 전투 보스(TestBoss) 루트 오브젝트. 이 오브젝트가 비활성화되면 BGM을 끕니다.")]
    [SerializeField] private GameObject testBossRoot;
    [Tooltip("보스가 비활성화되면 BGM 페이드아웃 시간(초)")]
    [SerializeField] private float bossBgmFadeOutOnBossEnd = 1.0f;

    [Header("컷씬 보스(연출용 TestBoss2)")]
    [Tooltip("씬에 꺼진 상태로 배치해둔 컷씬용 보스 오브젝트")]
    [SerializeField] private GameObject cutsceneBossRoot; // TestBoss2 Root (SetActive(false) 상태 권장)

    [Tooltip("컷씬 보스 Animator (비워두면 cutsceneBossRoot에서 자동으로 찾음)")]
    [SerializeField] private Animator cutsceneBossAnimator;

    [Tooltip("walk 상태 유지 시간")]
    [SerializeField] private float bossWalkSeconds = 3f;

    [Tooltip("3Phase_2 상태 유지 시간")]
    [SerializeField] private float bossPhase2Seconds = 2f;

    [Tooltip("Animator State 이름 (정확히 Animator에 있는 state 이름과 동일해야 함)")]
    [SerializeField] private string walkStateName = "walk";
    [SerializeField] private string phase2StateName = "3Phase_2";

    [Tooltip("컷씬 보스가 켜질 때, AI/전투 스크립트가 있으면 같이 꺼버리기(원치 않는 행동 방지)")]
    [SerializeField] private MonoBehaviour[] cutsceneBossScriptsToDisable;

    private bool played;
    private float prevTimeScale = 1f;

    private Coroutine bossEndWatchCo;
    private bool bossBgmStarted;

    private void Reset()
    {
        var col = GetComponent<Collider>();
        if (col) col.isTrigger = true;
    }

    private void OnDisable()
    {
        if (bossEndWatchCo != null)
        {
            StopCoroutine(bossEndWatchCo);
            bossEndWatchCo = null;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (playOnlyOnce && played) return;
        if (!other.CompareTag("Player")) return;

        played = true;
        StartCoroutine(CoPlay());
    }

    private IEnumerator CoPlay()
    {
        // 0) 컷씬 딜레이(보스 생성 타이밍 맞추기)
        if (cutsceneDelay > 0f)
        {
            float t0 = 0f;
            while (t0 < cutsceneDelay)
            {
                t0 += Time.deltaTime;
                yield return null;
            }
        }

        if (!mainCamera) mainCamera = Camera.main;

        if (!mainCamera || !cutsceneCamera || points == null || points.Length < 2)
        {
            Debug.LogError("[BossCutsceneTrigger_A] 세팅 누락(mainCamera/cutsceneCamera/points>=2)");
            yield break;
        }

        // 1) 보스 BGM 시작 (컷씬 시작부터)
        if (bossBgmClip != null)
        {
            yield return StartCoroutine(CoPlayBossBgm());
            bossBgmStarted = true;

            // ✅ “testboss 비활성화될 때까지” 자동 감시 시작
            if (bossEndWatchCo == null)
                bossEndWatchCo = StartCoroutine(CoWatchBossEndAndStopBgm());
        }

        // 2) 입력 잠금(스크립트 비활성화)
        if (disableWhileCutscene != null)
        {
            foreach (var s in disableWhileCutscene)
                if (s) s.enabled = false;
        }

        // 3) UI 끄기
        if (uiToDisableWhileCutscene != null)
        {
            foreach (var go in uiToDisableWhileCutscene)
                if (go) go.SetActive(false);
        }

        // 4) TimeScale 조절(0~1)
        prevTimeScale = Time.timeScale;
        Time.timeScale = Mathf.Clamp01(cutsceneTimeScale);

        // 5) 컷씬 카메라 전환
        mainCamera.gameObject.SetActive(false);
        cutsceneCamera.gameObject.SetActive(true);

        // 시작 포인트
        cutsceneCamera.transform.position = points[0].position;
        cutsceneCamera.transform.rotation = points[0].rotation;

        // 6) 컷씬 보스 연출 시작(동시에 진행)
        Coroutine bossCo = null;
        if (cutsceneBossRoot != null)
            bossCo = StartCoroutine(CoPlayCutsceneBoss());

        // 시작 포인트 홀드
        yield return HoldAtPoint(0);

        // 7) 카메라 경로 이동
        for (int seg = 0; seg < points.Length - 1; seg++)
        {
            float dur = GetMoveDuration(seg);
            Transform a = points[seg];
            Transform b = points[seg + 1];

            float t = 0f;
            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                float lerp = Mathf.Clamp01(t / Mathf.Max(0.01f, dur));

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

                if (Input.GetKeyDown(skipKey))
                {
                    // 스킵 시 컷씬 보스도 정리
                    if (cutsceneBossRoot) cutsceneBossRoot.SetActive(false);
                    seg = points.Length;
                    break;
                }

                yield return null;
            }

            yield return HoldAtPoint(seg + 1);
        }

        // 8) 컷씬 보스 코루틴이 아직 돌고 있으면 기다려줌(선택)
        if (bossCo != null)
            yield return bossCo;

        // 9) 원복
        cutsceneCamera.gameObject.SetActive(false);
        mainCamera.gameObject.SetActive(true);

        Time.timeScale = prevTimeScale;

        if (uiToDisableWhileCutscene != null)
        {
            foreach (var go in uiToDisableWhileCutscene)
                if (go) go.SetActive(true);
        }

        if (disableWhileCutscene != null)
        {
            foreach (var s in disableWhileCutscene)
                if (s) s.enabled = true;
        }

        // ✅ 여기서는 BGM을 끄지 않는다!
        // BGM은 “testBossRoot가 비활성화될 때” 코루틴이 끈다.
    }

    private IEnumerator CoPlayCutsceneBoss()
    {
        cutsceneBossRoot.SetActive(true);

        if (!cutsceneBossAnimator)
            cutsceneBossAnimator = cutsceneBossRoot.GetComponentInChildren<Animator>(true);

        if (!cutsceneBossAnimator)
        {
            Debug.LogWarning("[BossCutsceneTrigger_A] cutsceneBossAnimator를 찾지 못했음 (TestBoss2에 Animator 없음?)");
            yield break;
        }

        if (cutsceneBossScriptsToDisable != null)
        {
            foreach (var s in cutsceneBossScriptsToDisable)
                if (s) s.enabled = false;
        }

        cutsceneBossAnimator.Play(walkStateName, 0, 0f);
        yield return WaitUnscaled(bossWalkSeconds);

        cutsceneBossAnimator.Play(phase2StateName, 0, 0f);
        yield return WaitUnscaled(bossPhase2Seconds);

        cutsceneBossRoot.SetActive(false);
    }

    private IEnumerator CoWatchBossEndAndStopBgm()
    {
        // testBossRoot가 없으면 감지 불가
        if (testBossRoot == null)
        {
            Debug.LogWarning("[BossCutsceneTrigger_A] Test Boss Root가 비어있어서 BGM 종료 감지를 할 수 없습니다.");
            yield break;
        }

        // 보스가 “활성화 되기 전” 구간도 있을 수 있으니,
        // 1) 먼저 활성화 될 때까지 기다렸다가
        // 2) 그 다음 비활성화 될 때를 기다린다.
        while (testBossRoot != null && !testBossRoot.activeInHierarchy)
        {
            yield return null;
        }

        // 이제 보스가 켜졌으니, 꺼질 때까지 대기
        while (testBossRoot != null && testBossRoot.activeInHierarchy)
        {
            yield return null;
        }

        // 보스가 꺼졌다 -> BGM 종료
        if (bossBgmStarted)
        {
            yield return StartCoroutine(CoStopBossBgm());
            bossBgmStarted = false;
        }

        bossEndWatchCo = null;
    }

    private IEnumerator CoStopBossBgm()
    {
        if (!bgmSource) yield break;
        if (!bgmSource.isPlaying) yield break;

        float fade = Mathf.Max(0f, bossBgmFadeOutOnBossEnd);

        if (fade > 0f)
        {
            float startVol = bgmSource.volume;
            float t = 0f;
            while (t < fade)
            {
                t += Time.unscaledDeltaTime;
                bgmSource.volume = Mathf.Lerp(startVol, 0f, t / fade);
                yield return null;
            }
        }

        bgmSource.Stop();
        bgmSource.volume = bossBgmVolume; // 다음에 다시 재생될 때를 대비해 복구
    }

    private IEnumerator WaitUnscaled(float seconds)
    {
        if (seconds <= 0f) yield break;
        float t = 0f;
        while (t < seconds)
        {
            if (Input.GetKeyDown(skipKey)) yield break;
            t += Time.unscaledDeltaTime;
            yield return null;
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
            if (Input.GetKeyDown(skipKey)) yield break;
            yield return null;
        }
    }

    private IEnumerator CoPlayBossBgm()
    {
        if (!bgmSource)
        {
            var go = GameObject.Find("BGM_Source");
            if (go != null) bgmSource = go.GetComponent<AudioSource>();

            if (!bgmSource)
            {
                var newGo = new GameObject("BGM_Source");
                bgmSource = newGo.AddComponent<AudioSource>();
                bgmSource.loop = true;
                bgmSource.playOnAwake = false;
                DontDestroyOnLoad(newGo);
            }
        }

        if (bgmSource.clip == bossBgmClip && bgmSource.isPlaying)
        {
            bgmSource.volume = bossBgmVolume;
            yield break;
        }

        // 이전 BGM이 있다면 페이드아웃
        if (bgmSource.isPlaying && bossBgmFadeOutPrev > 0f)
        {
            float startVol = bgmSource.volume;
            float t = 0f;
            while (t < bossBgmFadeOutPrev)
            {
                t += Time.unscaledDeltaTime;
                bgmSource.volume = Mathf.Lerp(startVol, 0f, t / bossBgmFadeOutPrev);
                yield return null;
            }
        }

        bgmSource.Stop();
        bgmSource.clip = bossBgmClip;
        bgmSource.volume = (bossBgmFadeIn > 0f) ? 0f : bossBgmVolume;
        bgmSource.loop = true;
        bgmSource.Play();

        if (bossBgmFadeIn > 0f)
        {
            float t = 0f;
            while (t < bossBgmFadeIn)
            {
                t += Time.unscaledDeltaTime;
                bgmSource.volume = Mathf.Lerp(0f, bossBgmVolume, t / bossBgmFadeIn);
                yield return null;
            }
            bgmSource.volume = bossBgmVolume;
        }
    }
}
