using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

public class ParryPostFxPulse : MonoBehaviour
{
    public static ParryPostFxPulse Instance { get; private set; }

    [Header("Target Volume (Parry FX 전용)")]
    [SerializeField] private Volume volume;

    [Header("Pulse")]
    [Range(0f, 1f)]
    [SerializeField] private float peakWeight = 1f;

    [Tooltip("0 -> peak")]
    [SerializeField] private float rampIn = 0.02f;

    [Tooltip("peak 유지(Realtime)")]
    [SerializeField] private float hold = 0.06f;

    [Tooltip("peak -> 0")]
    [SerializeField] private float rampOut = 0.10f;

    [Header("Options")]
    [SerializeField] private bool ignoreWhenPaused = true;

    private Coroutine co;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;

        if (volume == null) volume = GetComponent<Volume>();
        if (volume != null) volume.weight = 0f;
    }

    public static void Play()
    {
        if (Instance == null) return;
        Instance.PlayInternal();
    }

    private void PlayInternal()
    {
        if (volume == null) return;
        if (ignoreWhenPaused && Mathf.Approximately(Time.timeScale, 0f)) return;

        if (co != null) StopCoroutine(co);
        co = StartCoroutine(PulseRoutine());
    }

    private IEnumerator PulseRoutine()
    {
        // 0 -> peak
        float inDur = Mathf.Max(0.001f, rampIn);
        float t = 0f;
        while (t < inDur)
        {
            t += Time.unscaledDeltaTime;
            volume.weight = Mathf.Lerp(0f, peakWeight, t / inDur);
            yield return null;
        }
        volume.weight = peakWeight;

        // hold
        if (hold > 0f)
            yield return new WaitForSecondsRealtime(hold);

        // peak -> 0
        float outDur = Mathf.Max(0.001f, rampOut);
        t = 0f;
        while (t < outDur)
        {
            t += Time.unscaledDeltaTime;
            volume.weight = Mathf.Lerp(peakWeight, 0f, t / outDur);
            yield return null;
        }
        volume.weight = 0f;

        co = null;
    }

    private void OnDisable()
    {
        if (volume != null) volume.weight = 0f;
    }
}
