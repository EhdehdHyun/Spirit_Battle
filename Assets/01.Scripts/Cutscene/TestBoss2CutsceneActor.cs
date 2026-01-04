using System.Collections;
using UnityEngine;

public class TestBoss2CutsceneActor : MonoBehaviour
{
    [Header("Animator")]
    [SerializeField] private Animator animator;

    [Header("State Names (Animator 상태 이름과 동일해야 함)")]
    [SerializeField] private string walkState = "walk";
    [SerializeField] private string phase2State = "3Phase_2";

    [Header("Durations (초)")]
    [SerializeField] private float walkTime = 3f;
    [SerializeField] private float phase2Time = 2f;

    [Header("옵션")]
    [SerializeField] private bool useUnscaledTime = true; // 컷씬 timeScale 조절해도 고정 시간으로 재생

    private Coroutine co;

    private void Reset()
    {
        animator = GetComponentInChildren<Animator>();
    }

    private void OnEnable()
    {
        if (co != null) StopCoroutine(co);
        co = StartCoroutine(CoPlay());
    }

    private IEnumerator CoPlay()
    {
        if (!animator) animator = GetComponentInChildren<Animator>();
        if (!animator)
        {
            Debug.LogError("[TestBoss2CutsceneActor] Animator가 없습니다.");
            yield break;
        }

        // 1) Walk 3초
        animator.Play(walkState, 0, 0f);
        yield return Wait(walkTime);

        // 2) 3Phase_2 2초
        animator.Play(phase2State, 0, 0f);
        yield return Wait(phase2Time);

        // 3) 컷씬용 보스 비활성화
        gameObject.SetActive(false);
    }

    private IEnumerator Wait(float seconds)
    {
        if (seconds <= 0f) yield break;

        float t = 0f;
        while (t < seconds)
        {
            t += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            yield return null;
        }
    }
}
