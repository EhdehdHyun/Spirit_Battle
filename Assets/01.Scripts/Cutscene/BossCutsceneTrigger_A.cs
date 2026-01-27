using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class BossCutsceneTrigger_A : MonoBehaviour
{
    [Header("카메라")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Camera cutsceneCamera;

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
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioClip bossBgmClip;
    [Range(0f, 1f)]
    [SerializeField] private float bossBgmVolume = 1f;
    [SerializeField] private float bossBgmFadeIn = 0.5f;
    [SerializeField] private float bossBgmFadeOutPrev = 0.3f;

    [Header("컷씬 보스 SFX")]
    [SerializeField] private AudioSource cutsceneBossSfxSource;
    [SerializeField] private AudioClip bossArmsOpenClip;

    [Range(0f, 1f)][SerializeField] private float bossArmsOpenVolume = 1f;
    [SerializeField] private float bossArmsOpenDelay = 0.0f;

    [Header("보스전 BGM 종료 조건(필수)")]
    [SerializeField] private GameObject testBossRoot;
    [SerializeField] private float bossBgmFadeOutOnBossEnd = 1.0f;

    [Header("컷씬 보스(연출용 TestBoss2)")]
    [SerializeField] private GameObject cutsceneBossRoot;
    [SerializeField] private Animator cutsceneBossAnimator;
    [SerializeField] private float bossWalkSeconds = 3f;
    [SerializeField] private float bossPhase2Seconds = 2f;
    [SerializeField] private string walkStateName = "walk";
    [SerializeField] private string phase2StateName = "3Phase_2";
    [SerializeField] private MonoBehaviour[] cutsceneBossScriptsToDisable;

    [Header("Spawn Bridge (스킵 시 즉시 소환)")]
    [SerializeField] private BossSpawnInteratableOnce bossPortal;

    [Header("Letterbox UI (컷씬에서만 켜짐)")]
    [SerializeField] private GameObject letterboxRoot;
    [SerializeField] private RectTransform letterboxTop;
    [SerializeField] private RectTransform letterboxBottom;
    [SerializeField] private float letterboxHeight = 160f;
    [SerializeField] private float letterboxAnimTime = 0.25f;
    [SerializeField] private float letterboxPreRoll = 0.0f;
    [SerializeField] private float letterboxPostRoll = 0.0f;

    private float topBaseH;
    private float bottomBaseH;

    private bool played;
    private float prevTimeScale = 1f;

    private Coroutine bossEndWatchCo;
    private bool bossBgmStarted;
    private bool isSkipRequested = false;

    private void Awake()
    {
        CacheLetterboxBase();
        SetLetterboxInstant(false);
    }

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

    public void OnSkipButtonClicked()
    {
        isSkipRequested = true;
    }

    private IEnumerator CoPlay()
    {
        isSkipRequested = false;
        try
        {
            UIVisibilityManager.Instance?.HideAllExceptGameOver();
            QuestHUDUI.Instance?.gameObject.SetActive(false);
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
                yield break;
            }

            if (bossBgmClip != null)
            {
                yield return StartCoroutine(CoPlayBossBgm());
                bossBgmStarted = true;

                if (bossEndWatchCo == null)
                    bossEndWatchCo = StartCoroutine(CoWatchBossEndAndStopBgm());
            }

            if (disableWhileCutscene != null)
            {
                foreach (var s in disableWhileCutscene)
                    if (s) s.enabled = false;
            }

            if (uiToDisableWhileCutscene != null)
            {
                foreach (var go in uiToDisableWhileCutscene)
                    if (go) go.SetActive(false);
            }

            if (letterboxPreRoll > 0f)
                yield return WaitUnscaled(letterboxPreRoll);

            yield return AnimateLetterbox(true);

            prevTimeScale = Time.timeScale;
            Time.timeScale = Mathf.Clamp01(cutsceneTimeScale);

            mainCamera.gameObject.SetActive(false);
            cutsceneCamera.gameObject.SetActive(true);

            cutsceneCamera.transform.position = points[0].position;
            cutsceneCamera.transform.rotation = points[0].rotation;

            Coroutine bossCo = null;
            if (cutsceneBossRoot != null)
                bossCo = StartCoroutine(CoPlayCutsceneBoss());

            yield return HoldAtPoint(0);

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

                    if (Input.GetKeyDown(skipKey) || isSkipRequested)
                    {
                        if (cutsceneBossRoot) cutsceneBossRoot.SetActive(false);
                        seg = points.Length;
                        break;
                    }

                    yield return null;
                }

                if (Input.GetKeyDown(skipKey) || isSkipRequested) break;

                yield return HoldAtPoint(seg + 1);

                if (Input.GetKeyDown(skipKey) || isSkipRequested) break;
            }

            if (bossCo != null)
                yield return bossCo;

            cutsceneCamera.gameObject.SetActive(false);
            mainCamera.gameObject.SetActive(true);

            Time.timeScale = prevTimeScale;

            if (letterboxPostRoll > 0f)
                yield return WaitUnscaled(letterboxPostRoll);

            yield return AnimateLetterbox(false);

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
        }
        finally
        {
            GlobalInputBlocker.SetKeyboardBlocked(false);
            UIVisibilityManager.Instance?.RestoreAll();
            QuestHUDUI.Instance?.gameObject.SetActive(true);
            isSkipRequested = false;
        }
    }

    private IEnumerator CoPlayCutsceneBoss()
    {
        cutsceneBossRoot.SetActive(true);

        if (!cutsceneBossAnimator)
            cutsceneBossAnimator = cutsceneBossRoot.GetComponentInChildren<Animator>(true);

        if (!cutsceneBossAnimator)
        {
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
        StartCoroutine(CoPlayBossArmsOpenSfx());
        yield return WaitUnscaled(bossPhase2Seconds);

        cutsceneBossRoot.SetActive(false);
    }

    private IEnumerator CoWatchBossEndAndStopBgm()
    {
        if (testBossRoot == null)
        {
            yield break;
        }

        while (testBossRoot != null && !testBossRoot.activeInHierarchy)
            yield return null;

        while (testBossRoot != null && testBossRoot.activeInHierarchy)
            yield return null;

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
        bgmSource.volume = bossBgmVolume;
    }

    private IEnumerator WaitUnscaled(float seconds)
    {
        if (seconds <= 0f) yield break;
        float t = 0f;
        while (t < seconds)
        {
            if (Input.GetKeyDown(skipKey) || isSkipRequested) yield break;
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
            if (Input.GetKeyDown(skipKey) || isSkipRequested) yield break;
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

    private void CacheLetterboxBase()
    {
        if (letterboxTop) topBaseH = letterboxTop.sizeDelta.y;
        if (letterboxBottom) bottomBaseH = letterboxBottom.sizeDelta.y;
    }

    private void SetLetterboxInstant(bool on)
    {
        if (letterboxRoot) letterboxRoot.SetActive(on);

        if (letterboxTop)
        {
            var s = letterboxTop.sizeDelta;
            s.y = on ? (topBaseH + letterboxHeight) : topBaseH;
            letterboxTop.sizeDelta = s;
        }

        if (letterboxBottom)
        {
            var s = letterboxBottom.sizeDelta;
            s.y = on ? (bottomBaseH + letterboxHeight) : bottomBaseH;
            letterboxBottom.sizeDelta = s;
        }

        ForceImageVisible(letterboxTop);
        ForceImageVisible(letterboxBottom);
    }

    private IEnumerator AnimateLetterbox(bool show)
    {
        if (show && letterboxRoot) letterboxRoot.SetActive(true);

        if (!letterboxTop && !letterboxBottom)
            yield break;

        ForceImageVisible(letterboxTop);
        ForceImageVisible(letterboxBottom);

        float dur = Mathf.Max(0.01f, letterboxAnimTime);
        float t = 0f;

        float fromTop = letterboxTop ? letterboxTop.sizeDelta.y : 0f;
        float fromBottom = letterboxBottom ? letterboxBottom.sizeDelta.y : 0f;

        float toTop = letterboxTop ? (show ? (topBaseH + letterboxHeight) : topBaseH) : 0f;
        float toBottom = letterboxBottom ? (show ? (bottomBaseH + letterboxHeight) : bottomBaseH) : 0f;

        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float lerp = Mathf.Clamp01(t / dur);

            if (letterboxTop)
            {
                var s = letterboxTop.sizeDelta;
                s.y = Mathf.Lerp(fromTop, toTop, lerp);
                letterboxTop.sizeDelta = s;
            }

            if (letterboxBottom)
            {
                var s = letterboxBottom.sizeDelta;
                s.y = Mathf.Lerp(fromBottom, toBottom, lerp);
                letterboxBottom.sizeDelta = s;
            }

            yield return null;
        }

        if (letterboxTop)
        {
            var s = letterboxTop.sizeDelta;
            s.y = toTop;
            letterboxTop.sizeDelta = s;
        }

        if (letterboxBottom)
        {
            var s = letterboxBottom.sizeDelta;
            s.y = toBottom;
            letterboxBottom.sizeDelta = s;
        }

        if (!show && letterboxRoot)
            letterboxRoot.SetActive(false);
    }

    private void ForceImageVisible(RectTransform rt)
    {
        if (!rt) return;
        var img = rt.GetComponent<Image>();
        if (!img) return;

        var c = img.color;
        if (c.a <= 0.01f) c.a = 1f;
        img.color = c;
        img.enabled = true;
    }

    private AudioSource GetBossSfxSource()
    {
        if (cutsceneBossSfxSource) return cutsceneBossSfxSource;
        if (!cutsceneBossRoot) return null;

        cutsceneBossSfxSource = cutsceneBossRoot.GetComponentInChildren<AudioSource>(true);
        if (!cutsceneBossSfxSource)
        {
            cutsceneBossSfxSource = cutsceneBossRoot.AddComponent<AudioSource>();
            cutsceneBossSfxSource.playOnAwake = false;
        }
        return cutsceneBossSfxSource;
    }

    private IEnumerator CoPlayBossArmsOpenSfx()
    {
        if (!bossArmsOpenClip) yield break;

        if (bossArmsOpenDelay > 0f)
            yield return WaitUnscaled(bossArmsOpenDelay);

        var src = GetBossSfxSource();
        if (!src) yield break;

        src.PlayOneShot(bossArmsOpenClip, bossArmsOpenVolume);
    }
}