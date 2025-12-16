using UnityEngine;
using System.Collections;

public class IntroCameraController : MonoBehaviour
{
    public Transform lookTarget;
    private Quaternion startRot;
    private Quaternion normalRot;

    void Start()
    {
        startRot = transform.rotation;
        normalRot = Quaternion.Euler(5, 0, 0);
        StartCoroutine(RecoverView());
    }
    

    IEnumerator RecoverView()
    {
        // 눈 거의 다 뜬 뒤
        yield return new WaitForSeconds(6.5f);
        
        Quaternion lookRot =
            Quaternion.LookRotation(
                (lookTarget.position - transform.position).normalized
            );

        // NPC를 바라보되, 위를 보도록 X축 보정
        Vector3 euler = lookRot.eulerAngles;
        euler.x -= 10f; // 위를 보게 
        lookRot = Quaternion.Euler(euler);

        // NPC 쪽으로 회전
        yield return Rotate(startRot, lookRot, 2.5f);

        yield return new WaitForSeconds(2.0f);

        // 다시 원래 시야로
        yield return Rotate(lookRot, startRot, 3.0f);
        
    }
    
    IEnumerator Rotate(Quaternion from, Quaternion to, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            transform.rotation = Quaternion.Slerp(from, to, t / duration);
            yield return null;
        }
    }

}