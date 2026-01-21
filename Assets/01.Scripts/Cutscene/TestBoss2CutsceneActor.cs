using System.Collections;
using UnityEngine;

public class TestBoss2CutsceneActor : MonoBehaviour
{
    [Header("Animator")]
    [SerializeField] private Animator animator;

    [Header("State Names (Animator ���� �̸��� �����ؾ� ��)")]
    [SerializeField] private string walkState = "walk";
    [SerializeField] private string phase2State = "3Phase_2";

    [Header("Durations (��)")]
    [SerializeField] private float walkTime = 3f;
    [SerializeField] private float phase2Time = 2f;

    [Header("�ɼ�")]
    [SerializeField] private bool useUnscaledTime = true; // �ƾ� timeScale �����ص� ���� �ð����� ���

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
            yield break;
        }

        // 1) Walk 3��
        animator.Play(walkState, 0, 0f);
        yield return Wait(walkTime);

        // 2) 3Phase_2 2��
        animator.Play(phase2State, 0, 0f);
        yield return Wait(phase2Time);

        // 3) �ƾ��� ���� ��Ȱ��ȭ
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
