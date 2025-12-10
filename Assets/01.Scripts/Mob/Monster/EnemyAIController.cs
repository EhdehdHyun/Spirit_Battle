using UnityEngine;

public class EnemyAIController : MonoBehaviour
{
    public enum AIState
    {
        Idle,
        Chase,
        Attack,
        Dead
    }
}
