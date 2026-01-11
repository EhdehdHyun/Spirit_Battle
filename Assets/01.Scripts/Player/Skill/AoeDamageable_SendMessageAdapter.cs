using UnityEngine;

public class AoeDamageable_SendMessageAdapter : MonoBehaviour, IAoeDamageable
{
    [Tooltip("적 오브젝트가 실제로 가지고 있는 데미지 메서드 이름")]
    [SerializeField] private string methodName = "TakeDamage";

    public void ApplyAoeDamage(float damage, Transform attacker)
    {
        gameObject.SendMessage(methodName, damage, SendMessageOptions.DontRequireReceiver);
    }
}
