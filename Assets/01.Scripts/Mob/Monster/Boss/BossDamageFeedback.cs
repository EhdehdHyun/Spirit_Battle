using System.Collections;
using UnityEngine;

public class BossDamageFeedback : MonoBehaviour
{
    [Header("흔들 대상(비주얼 루트)")]
    [SerializeField] private Transform visualRoot;

    [Header("설정")]
    [SerializeField] private float duration = 0.12f;
    [SerializeField] private float positionAmplitude = 0.05f;
    [SerializeField] private float rotationAmplitude = 2.0f;
    [SerializeField] private float frequency = 35f;

    [Header("옵션")]
    [SerializeField] private bool useUnscaledTime = false;

    private Coroutine shakeCo;
    private Vector3 originLocalPos;
    private Quaternion originLocalRot;

    private void Awake()
    {
        if (visualRoot == null)
            visualRoot = transform;

        originLocalPos = visualRoot.localPosition;
        originLocalRot = visualRoot.localRotation;
    }

    public void Play()
    {
        if (visualRoot == null) return;

        if (shakeCo != null)
            StopCoroutine(shakeCo);

        shakeCo = StartCoroutine(ShakeRoutine());
    }

    public void StopAndReset()
    {
        if (shakeCo != null)
            StopCoroutine(shakeCo);

        shakeCo = null;

        if (visualRoot != null)
        {
            visualRoot.localPosition = originLocalPos;
            visualRoot.localRotation = originLocalRot;
        }
    }

    private IEnumerator ShakeRoutine()
    {
        float t = 0f;

        // 혹시 중간에 리깅/애니가 local 값 바꿀 수 있어서 시작값 갱신
        originLocalPos = visualRoot.localPosition;
        originLocalRot = visualRoot.localRotation;

        while (t < duration)
        {
            float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            t += dt;

            // 진폭 감소(처음 강하고 끝에 약하게)
            float fade = 1f - (t / duration);

            float n1 = Mathf.PerlinNoise(Time.time * frequency, 0.1f) * 2f - 1f;
            float n2 = Mathf.PerlinNoise(0.2f, Time.time * frequency) * 2f - 1f;
            float n3 = Mathf.PerlinNoise(Time.time * frequency, 0.7f) * 2f - 1f;

            Vector3 posOffset = new Vector3(n1, 0f, n2) * (positionAmplitude * fade);
            Vector3 rotOffset = new Vector3(0f, n3, 0f) * (rotationAmplitude * fade);

            visualRoot.localPosition = originLocalPos + posOffset;
            visualRoot.localRotation = originLocalRot * Quaternion.Euler(rotOffset);

            yield return null;
        }

        visualRoot.localPosition = originLocalPos;
        visualRoot.localRotation = originLocalRot;
        shakeCo = null;
    }
}
