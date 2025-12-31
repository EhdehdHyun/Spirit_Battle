using System.Collections;
using UnityEngine;

public class ParryTelegraphShrinkRing : MonoBehaviour
{
    [Header("Follow (공격 손/무기 본)")]
    [SerializeField] private Transform followTarget;
    [SerializeField] private Vector3 worldOffset = Vector3.zero;

    [Header("Rings (SpriteRenderer/Transform)")]
    [SerializeField] private Transform outerRing;   // Big_Ring (줄어드는 링)
    [SerializeField] private Transform innerRing;   // Small_Ring (고정 링)
    [SerializeField] private GameObject rootObject; // Ring 부모(통째로 켜고 끄기)

    [Header("Timings")]
    [Tooltip("공격 시작 ~ 패링 윈도우 ON까지, 큰 링이 줄어드는 시간")]
    [SerializeField] private float shrinkSeconds = 0.35f;

    [Tooltip("패링 윈도우 OFF 이후 사라지는 시간(알파 페이드)")]
    [SerializeField] private float fadeOutSeconds = 0.2f;

    [Header("Scale")]
    [Tooltip("큰 링 시작 크기 배율 (innerRing 기준)")]
    [SerializeField] private float outerStartScaleMul = 1.8f;

    [Header("Billboard (카메라 바라보기)")]
    [SerializeField] private bool faceCamera = true;
    [Tooltip("true면 Y축 회전만(바닥 고정), false면 완전 카메라 정면")]
    [SerializeField] private bool lockYAxis = true;
    [SerializeField] private Camera targetCamera;

    [Header("Options")]
    [SerializeField] private bool useUnscaledTime = true;
    [SerializeField] private bool hideOnDisable = true;

    private Coroutine co;
    private bool windowOpen;

    private SpriteRenderer[] renderers;

    private void Awake()
    {
        if (rootObject == null) rootObject = gameObject;
        if (targetCamera == null) targetCamera = Camera.main;

        CacheRenderers();
        HideImmediate();
    }

    private void OnEnable()
    {
        CacheRenderers();
    }

    private void OnDisable()
    {
        if (hideOnDisable) HideImmediate();
    }

    private void CacheRenderers()
    {
        if (rootObject == null) return;
        renderers = rootObject.GetComponentsInChildren<SpriteRenderer>(true);
    }

    private void LateUpdate()
    {
        if (rootObject == null || !rootObject.activeSelf) return;

        if (followTarget != null)
            rootObject.transform.position = followTarget.position + worldOffset;

        if (faceCamera && targetCamera != null)
        {
            if (lockYAxis)
            {
                Vector3 toCam = (rootObject.transform.position - targetCamera.transform.position);
                toCam.y = 0f;
                if (toCam.sqrMagnitude > 0.0001f)
                    rootObject.transform.rotation = Quaternion.LookRotation(toCam.normalized, Vector3.up);
            }
            else
            {
                Vector3 dir = (rootObject.transform.position - targetCamera.transform.position).normalized;
                if (dir.sqrMagnitude > 0.0001f)
                    rootObject.transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
            }
        }
    }

    // =========================================================
    // ✅ MonsterParryHandler / Animation Event 에서 찾는 "이름"
    // =========================================================
    public void TelegraphStart() => Anim_TelegraphStart();
    public void TelegraphEnd() => Anim_TelegraphEnd();
    public void ParryWindowOn() => Anim_ParryWindowOn();
    public void ParryWindowOff() => Anim_ParryWindowOff();

    // 패링 성공하면 즉시 숨김(원 바로 사라지게)
    public void ParrySuccessHide() => HideImmediate();

    // =========================================================
    // ✅ 내부 동작(원래 Anim_ 네이밍)
    // =========================================================
    public void Anim_TelegraphStart()
    {
        windowOpen = false;
        Show();

        Vector3 target = (innerRing != null) ? innerRing.localScale : Vector3.one;

        if (outerRing != null) outerRing.localScale = target * outerStartScaleMul;
        if (innerRing != null) innerRing.localScale = target;

        SetAlphaAll(1f);

        RestartCo(ShrinkRoutine(target));
    }

    public void Anim_ParryWindowOn()
    {
        windowOpen = true;

        if (outerRing != null && innerRing != null)
            outerRing.localScale = innerRing.localScale;
    }

    public void Anim_ParryWindowOff()
    {
        windowOpen = false;
        RestartCo(FadeOutRoutine());
    }

    public void Anim_TelegraphEnd()
    {
        HideImmediate();
    }

    // =========================================================

    private IEnumerator ShrinkRoutine(Vector3 targetScale)
    {
        if (outerRing == null)
        {
            co = null;
            yield break;
        }

        float dur = Mathf.Max(0.01f, shrinkSeconds);
        float t = 0f;

        Vector3 startScale = targetScale * outerStartScaleMul;

        while (t < dur && !windowOpen)
        {
            t += Dt();
            float a = Mathf.Clamp01(t / dur);
            outerRing.localScale = Vector3.Lerp(startScale, targetScale, a);
            yield return null;
        }

        if (!windowOpen)
            outerRing.localScale = targetScale;

        co = null;
    }

    private IEnumerator FadeOutRoutine()
    {
        float dur = Mathf.Max(0.01f, fadeOutSeconds);
        float t = 0f;

        while (t < dur)
        {
            t += Dt();
            float a = Mathf.Clamp01(t / dur);
            SetAlphaAll(Mathf.Lerp(1f, 0f, a));
            yield return null;
        }

        HideImmediate();
        co = null;
    }

    private void RestartCo(IEnumerator routine)
    {
        if (co != null) StopCoroutine(co);
        co = StartCoroutine(routine);
    }

    private void Show()
    {
        if (rootObject != null && !rootObject.activeSelf)
            rootObject.SetActive(true);

        if (renderers != null)
        {
            foreach (var r in renderers)
                if (r != null) r.enabled = true;
        }
    }

    private void HideImmediate()
    {
        if (co != null) StopCoroutine(co);
        co = null;

        SetAlphaAll(0f);

        if (rootObject != null)
            rootObject.SetActive(false);
    }

    private void SetAlphaAll(float alpha)
    {
        alpha = Mathf.Clamp01(alpha);

        if (renderers == null || renderers.Length == 0) CacheRenderers();
        if (renderers == null) return;

        foreach (var r in renderers)
        {
            if (r == null) continue;
            var c = r.color;
            c.a = alpha;
            r.color = c;
        }
    }

    private float Dt() => useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
}
