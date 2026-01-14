using UnityEngine;

public class StunEffectState : StateMachineBehaviour
{
    [Tooltip("여기에 아까 만든 파티클 오브젝트의 정확한 이름을 적으세요")]
    public string particleName = "StunEffect";

    private ParticleSystem stunParticle;

    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
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
            stunParticle.Play();
        }
    }

    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (stunParticle != null)
        {
            stunParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        }
    }
}