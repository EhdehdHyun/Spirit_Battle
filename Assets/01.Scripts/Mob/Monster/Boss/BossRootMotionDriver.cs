using System.Collections;
using UnityEngine;

public class BossRootMotionDriver : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Animator animator;
    [SerializeField] private Rigidbody rigidbodyRoot;

    [Header("옵션")]
    [SerializeField] private bool ignoreRootMotionY = true;
    [SerializeField] private bool preventPushXZ = true;
    [SerializeField] private bool preventPhysicsRotation = true;

    [Header("루트모션 배율")]
    [Min(0f)]
    public float SpeedMultiplier = 1f;

    [Header("루트모션 잠금(멈추기)")]
    public bool LockRootMotion = false;

    private void Reset()
    {
        animator = GetComponent<Animator>();
        rigidbodyRoot = GetComponentInParent<Rigidbody>();
    }

    private void Awake()
    {
        if (animator == null) animator = GetComponent<Animator>();
        if (rigidbodyRoot == null) rigidbodyRoot = GetComponentInParent<Rigidbody>();

        if (animator != null)
        {
            animator.applyRootMotion = true;
            animator.updateMode = AnimatorUpdateMode.AnimatePhysics;
        }

        if (rigidbodyRoot != null)
        {
            rigidbodyRoot.interpolation = RigidbodyInterpolation.Interpolate;
            rigidbodyRoot.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rigidbodyRoot.constraints |= RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        }
    }

    private void OnAnimatorMove()
    {
        if (animator == null || rigidbodyRoot == null) return;

        if (preventPushXZ)
        {
            Vector3 v = rigidbodyRoot.velocity;
            v.x = 0f;
            v.z = 0f;
            rigidbodyRoot.velocity = v;
        }

        if (preventPhysicsRotation)
            rigidbodyRoot.angularVelocity = Vector3.zero;

        if (LockRootMotion) return;

        Vector3 delta = animator.deltaPosition * SpeedMultiplier;
        if (ignoreRootMotionY) delta.y = 0f;

        rigidbodyRoot.MovePosition(rigidbodyRoot.position + delta);
    }
}
