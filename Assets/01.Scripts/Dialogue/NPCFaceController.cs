using System.Collections;
using UnityEngine;

public class NPCFaceController : MonoBehaviour
{
    [SerializeField] private float rotateSpeed = 5f;

    private Coroutine lookRoutine;

    public void LookAtTarget(Transform target)
    {
        if (lookRoutine != null)
            StopCoroutine(lookRoutine);

        lookRoutine = StartCoroutine(LookRoutine(target));
    }

    public void StopLook()
    {
        if (lookRoutine != null)
            StopCoroutine(lookRoutine);
    }

    IEnumerator LookRoutine(Transform target)
    {
        while (true)
        {
            Vector3 dir = target.position - transform.position;
            dir.y = 0f; //위아래 고정 

            if (dir.sqrMagnitude > 0.01f)
            {
                Quaternion targetRot = Quaternion.LookRotation(dir);
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    targetRot,
                    Time.deltaTime * rotateSpeed
                );
            }

            yield return null;
        }
    }
}
