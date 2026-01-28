using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class HitVignetteFx : MonoBehaviour
{
    public static HitVignetteFx Instance { get; private set; }

    [SerializeField] private Volume volume;

    [Header("Vignette 값")]
    [SerializeField, Range(0f, 1f)] private float peakIntensity = 0.45f;
    [SerializeField] private float riseTime = 0.06f;   // 올라가는 시간
    [SerializeField] private float fallTime = 0.25f;   // 내려오는 시간
    [SerializeField] private bool useUnscaledTime = true;

    private Vignette vignette;
    private Coroutine co;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (volume == null) volume = GetComponent<Volume>();

        if (volume != null && volume.profile != null)
            volume.profile.TryGet(out vignette);

        if (vignette != null)
        {
            // 기본값은 꺼져있음
            vignette.intensity.Override(0f);
        }
    }

    public void Play(float strength01 = 1f)
    {
        if (vignette == null) return;

        float target = Mathf.Clamp01(peakIntensity * Mathf.Clamp01(strength01));

        if (co != null) StopCoroutine(co);
        co = StartCoroutine(FlashRoutine(target));
    }

    private IEnumerator FlashRoutine(float target)
    {
        float start = vignette.intensity.value;

        // Rise
        float t = 0f;
        float r = Mathf.Max(0.0001f, riseTime);
        while (t < r)
        {
            t += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            vignette.intensity.Override(Mathf.Lerp(start, target, t / r));
            yield return null;
        }
        vignette.intensity.Override(target);

        // Fall
        t = 0f;
        float f = Mathf.Max(0.0001f, fallTime);
        while (t < f)
        {
            t += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            vignette.intensity.Override(Mathf.Lerp(target, 0f, t / f));
            yield return null;
        }

        vignette.intensity.Override(0f);
        co = null;
    }
}
