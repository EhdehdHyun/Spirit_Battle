using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemeyAIController : MonoBehaviour
{
    public enum AIState
    {
        Idle,
        Chase,
        Attack,
        Dead
    }

    [Header("AI 설정")]
    public float idleUpdateInterval = 0.5f; // idle 상태 일 때 타겟 체크 주기
    public float chaseUpdateInterval = 0.1f; //chase일 때 목적지 갱싱 주기
    public float loseTargetDistance = 20f; //이 거리 이상 멀어지면 타겟을 포기함 

}
