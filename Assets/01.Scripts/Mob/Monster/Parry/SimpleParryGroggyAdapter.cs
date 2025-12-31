using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class SimpleParryGroggyAdapter : MonoBehaviour, IParryGroggyController
{
    [SerializeField] private Animator animator;
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private Rigidbody rb;

    private bool groggyRunning;

    private void Awake()
    {
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (agent == null) agent = GetComponentInParent<NavMeshAgent>();
        if (rb == null) rb = GetComponentInParent<Rigidbody>();
    }

    public bool IsParryImmune => groggyRunning; // 이미 스턴 중이면 또 못 받게

    public void EnterParryGroggy(float duration, string triggerName)
    {
        if (!gameObject.activeInHierarchy) return;
        if (groggyRunning) return;

        StartCoroutine(GroggyRoutine(duration, triggerName));
    }

    private IEnumerator GroggyRoutine(float duration, string triggerName)
    {
        groggyRunning = true;

        if (agent != null) agent.isStopped = true;
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        if (animator != null && !string.IsNullOrEmpty(triggerName))
        {
            animator.ResetTrigger(triggerName);
            animator.SetTrigger(triggerName);
        }

        yield return new WaitForSeconds(duration);

        if (agent != null) agent.isStopped = false;

        groggyRunning = false;
    }
}
