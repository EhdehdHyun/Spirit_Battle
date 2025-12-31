using System.Collections;
using UnityEngine;

public class ParryTimeSlow : MonoBehaviour
{
    public static ParryTimeSlow Instance { get; private set; }

    [Header("Slow Settings")]
    [Range(0.01f, 1f)]
    [SerializeField] private float slowScale = 0.25f;

    [Tooltip("슬로우 유지 시간(Realtime)")]
    [SerializeField] private float slowDuration = 0.12f;

    [Tooltip("원복(Realtime) - 0이면 즉시 원복")]
    [SerializeField] private float recoverDuration = 0.06f;

    [Header("Options")]
    [SerializeField] private bool affectFixedDeltaTime = true;
    [SerializeField] private bool ignoreWhenPaused = true;

    private Coroutine co;
    private float baseFixedDeltaTime;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        baseFixedDeltaTime = Time.fixedDeltaTime;
    }

    public static void Play()
    {
        if (Instance == null) return;
        Instance.PlayInternal();
    }

    public void PlayInternal()
    {
        if (ignoreWhenPaused && Mathf.Approximately(Time.timeScale, 0f))
            return;

        if (co != null) StopCoroutine(co);
        co = StartCoroutine(SlowRoutine());
    }

    private IEnumerator SlowRoutine()
    {
        float original = Time.timeScale;

        SetTimeScale(slowScale);

        if (slowDuration > 0f)
            yield return new WaitForSecondsRealtime(slowDuration);

        if (recoverDuration <= 0f)
        {
            SetTimeScale(original);
            co = null;
            yield break;
        }

        float t = 0f;
        float dur = Mathf.Max(0.01f, recoverDuration);

        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float a = Mathf.Clamp01(t / dur);
            SetTimeScale(Mathf.Lerp(slowScale, original, a));
            yield return null;
        }

        SetTimeScale(original);
        co = null;
    }

    private void SetTimeScale(float s)
    {
        Time.timeScale = Mathf.Clamp(s, 0.01f, 1f);

        if (affectFixedDeltaTime)
            Time.fixedDeltaTime = baseFixedDeltaTime * Time.timeScale;
    }
}
