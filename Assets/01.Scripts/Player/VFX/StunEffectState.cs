using UnityEngine;

public class StunEffectState : StateMachineBehaviour
{
    [Tooltip("Hierarchy�� �ִ� ��ƼŬ ������Ʈ�� ��Ȯ�� �̸��� ��������")]
    public string particleName = "StunEffect";

    private ParticleSystem stunParticle;

    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        animator.SetBool("IsStun", true);
        if (stunParticle == null)
        {
            foreach (var p in animator.GetComponentsInChildren<ParticleSystem>(true))
            {
                if (p.name == particleName)
                {
                    stunParticle = p;
                    break;
                }
            }
        }

        if (stunParticle != null)
        {
            stunParticle.gameObject.SetActive(true);
            stunParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            stunParticle.Play();
        }
    }

    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        animator.SetBool("IsStun", false);

        if (stunParticle != null)
        {
            stunParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }
}