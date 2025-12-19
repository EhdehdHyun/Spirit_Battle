using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DamageFeedback : MonoBehaviour
{
    [Header("흔들 대상(비주얼 루트)")]
    [SerializeField] private Transform visualRoot;

    [Header("흔들림 설정")]
    [SerializeField] private float duration = 0.12f;
    [SerializeField] private float positionAmplitude = 0.05f;
    [SerializeField] private float rotationAmplitude = 2.0f;
    [SerializeField] private float frequency = 35f;

    [Header("설정 - 피격 플래시(빨갛게)")]
    [Tooltip("비워두면 visualRoot(또는 자기 자신) 하위 Renderer들을 사용")]
    [SerializeField] private Transform rendererRoot;
    [SerializeField] private bool useHitFlash = true;
    [SerializeField] private Color flashColor = new Color(1f, 0.2f, 0.2f, 1f);
    [Tooltip("빨간색이 유지되는 시간")]
    [SerializeField] private float flashHoldTime = 0.05f;
    [Tooltip("원래 색으로 돌아가는 시간")]
    [SerializeField] private float flashFadeTime = 0.10f;

    [Header("옵션")]
    [SerializeField] private bool useUnscaledTime = false;

    private Coroutine feedbackCo;
    private Vector3 originLocalPos;
    private Quaternion originLocalRot;

    private struct MatColorInfo
    {
        public Renderer renderer;
        public int materialIndex;
        public int colorPropId;     // _BaseColor or _Color
        public Color originalColor;
        public bool valid;
    }

    private readonly List<MatColorInfo> _colorInfos = new();
    private MaterialPropertyBlock _mpb;

    private static readonly int BASE_COLOR = Shader.PropertyToID("_BaseColor");
    private static readonly int COLOR = Shader.PropertyToID("_Color");

    private void Awake()
    {
        if (visualRoot == null)
            visualRoot = transform;

        originLocalPos = visualRoot.localPosition;
        originLocalRot = visualRoot.localRotation;

        _mpb = new MaterialPropertyBlock();

        CacheRendererColors();
    }

    private void CacheRendererColors()
    {
        _colorInfos.Clear();

        Transform root = rendererRoot != null ? rendererRoot : visualRoot;
        if (root == null) root = transform;

        var renderers = root.GetComponentsInChildren<Renderer>(true);
        foreach (var r in renderers)
        {
            // SkinnedMeshRenderer / MeshRenderer 모두 Renderer라 OK
            var mats = r.sharedMaterials;
            for (int i = 0; i < mats.Length; i++)
            {
                var m = mats[i];
                if (m == null) continue;

                int propId = 0;
                if (m.HasProperty(BASE_COLOR)) propId = BASE_COLOR;
                else if (m.HasProperty(COLOR)) propId = COLOR;

                if (propId == 0) continue;

                Color original = m.GetColor(propId);

                _colorInfos.Add(new MatColorInfo
                {
                    renderer = r,
                    materialIndex = i,
                    colorPropId = propId,
                    originalColor = original,
                    valid = true
                });
            }
        }
    }

    public void Play()
    {
        if (visualRoot == null) return;

        if (feedbackCo != null)
            StopCoroutine(feedbackCo);

        feedbackCo = StartCoroutine(FeedbackRoutine());
    }

    public void StopAndReset()
    {
        if (feedbackCo != null)
            StopCoroutine(feedbackCo);

        feedbackCo = null;

        if (visualRoot != null)
        {
            visualRoot.localPosition = originLocalPos;
            visualRoot.localRotation = originLocalRot;
        }

        ClearFlash();
    }

    private IEnumerator FeedbackRoutine()
    {
        float t = 0f;

        // 혹시 중간에 리깅/애니가 local 값 바꿀 수 있어서 시작값 갱신
        originLocalPos = visualRoot.localPosition;
        originLocalRot = visualRoot.localRotation;

        // 플래시 시작
        if (useHitFlash)
        {
            // 렌더러/머티리얼이 런타임에 바뀌는 경우 대비해서 한번 더 캐시 (원하면 제거해도 됨)
            if (_colorInfos.Count == 0) CacheRendererColors();
            ApplyFlashColor(flashColor);
        }

        // 먼저 hold
        float hold = flashHoldTime;
        while (useHitFlash && hold > 0f)
        {
            float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            hold -= dt;
            yield return null;
        }

        // 흔들림 + 플래시 페이드(원복) 동시에 진행
        float fadeT = 0f;

        while (t < duration || (useHitFlash && fadeT < flashFadeTime))
        {
            float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;

            // 흔들림
            if (t < duration)
            {
                t += dt;
                float fade = 1f - (t / Mathf.Max(0.0001f, duration));

                float n1 = Mathf.PerlinNoise(Time.time * frequency, 0.1f) * 2f - 1f;
                float n2 = Mathf.PerlinNoise(0.2f, Time.time * frequency) * 2f - 1f;
                float n3 = Mathf.PerlinNoise(Time.time * frequency, 0.7f) * 2f - 1f;

                Vector3 posOffset = new Vector3(n1, 0f, n2) * (positionAmplitude * fade);
                Vector3 rotOffset = new Vector3(0f, n3, 0f) * (rotationAmplitude * fade);

                visualRoot.localPosition = originLocalPos + posOffset;
                visualRoot.localRotation = originLocalRot * Quaternion.Euler(rotOffset);
            }

            // 플래시 페이드 (flashColor -> originalColor)
            if (useHitFlash && flashFadeTime > 0f && fadeT < flashFadeTime)
            {
                fadeT += dt;
                float a = Mathf.Clamp01(fadeT / flashFadeTime);
                ApplyFlashLerp(a);
            }

            yield return null;
        }

        // 원복
        visualRoot.localPosition = originLocalPos;
        visualRoot.localRotation = originLocalRot;

        if (useHitFlash)
            ClearFlash();

        feedbackCo = null;
    }

    private void ApplyFlashColor(Color c)
    {
        foreach (var info in _colorInfos)
        {
            if (!info.valid || info.renderer == null) continue;

            _mpb.Clear();
            info.renderer.GetPropertyBlock(_mpb, info.materialIndex);
            _mpb.SetColor(info.colorPropId, c);
            info.renderer.SetPropertyBlock(_mpb, info.materialIndex);
        }
    }

    private void ApplyFlashLerp(float t01)
    {
        foreach (var info in _colorInfos)
        {
            if (!info.valid || info.renderer == null) continue;

            Color c = Color.Lerp(flashColor, info.originalColor, t01);

            _mpb.Clear();
            info.renderer.GetPropertyBlock(_mpb, info.materialIndex);
            _mpb.SetColor(info.colorPropId, c);
            info.renderer.SetPropertyBlock(_mpb, info.materialIndex);
        }
    }

    private void ClearFlash()
    {
        // 원래 머티리얼 색으로 완전 복귀(override 제거)
        foreach (var info in _colorInfos)
        {
            if (!info.valid || info.renderer == null) continue;

            _mpb.Clear();
            info.renderer.SetPropertyBlock(_mpb, info.materialIndex);
        }
    }
}
