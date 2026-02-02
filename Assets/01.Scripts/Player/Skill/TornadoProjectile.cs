using System.Collections.Generic;
using UnityEngine;

public class TornadoProjectile : MonoBehaviour
{
    [Header("Visual (Pivot at ground)")]
    [Tooltip("바닥 피벗. 이 Transform만 스케일 줄이면 밑이 붙은 채로 줄어듦.")]
    [SerializeField] private Transform visualRoot;

    [Header("Move")]
    [SerializeField] private float speed = 10f;
    [SerializeField] private float maxDistance = 10f;

    [Tooltip("남은 거리가 이 값 이하가 되면 점점 작아지기 시작")]
    [SerializeField] private float shrinkStartDistance = 2f;

    [Header("AOE / Pull")]
    [SerializeField] private LayerMask targetMask;
    [SerializeField] private Vector3 centerOffset = new Vector3(0f, 1.0f, 0f);

    [SerializeField] private float basePullRadius = 4f;
    [SerializeField] private float baseDamageRadius = 3f;

    [Tooltip("끌어당김 최대 속도로 먼 거리에서는 약하게, 가까울수록 강하게 해줌")]
    [SerializeField] private float pullMaxSpeed = 6f;

    [Tooltip("가속도(ForceMode.Acceleration)")]
    [SerializeField] private float pullAcceleration = 40f;

    [Header("DOT")]
    [SerializeField] private float damagePerTick = 10f;
    [SerializeField] private float tickInterval = 0.5f;

    [Header("Performance")]
    [SerializeField] private int maxHits = 32;

    // runtime
    private Transform _owner;
    private Vector3 _dir;
    private float _traveled;

    private Collider[] _overlap;
    private readonly HashSet<int> _pulledThisFrame = new HashSet<int>();
    private readonly Dictionary<int, float> _nextTickAt = new Dictionary<int, float>(64);

    public void Init(
        Transform owner,
        Vector3 forward,
        LayerMask targets,
        float moveSpeed,
        float travelDistance,
        float shrinkStartDist,
        float pullRadius,
        float damageRadius,
        float pullMaxSpd,
        float pullAccel,
        float dmgPerTick,
        float tickIntv,
        Vector3 aoeCenterOffset,
        int maxHitCount
    )
    {
        _owner = owner;

        forward.y = 0f;
        _dir = forward.sqrMagnitude > 0.0001f ? forward.normalized : transform.forward;

        targetMask = targets;
        speed = moveSpeed;
        maxDistance = Mathf.Max(0.01f, travelDistance);
        shrinkStartDistance = Mathf.Clamp(shrinkStartDist, 0.01f, maxDistance);

        basePullRadius = Mathf.Max(0f, pullRadius);
        baseDamageRadius = Mathf.Max(0f, damageRadius);

        pullMaxSpeed = Mathf.Max(0f, pullMaxSpd);
        pullAcceleration = Mathf.Max(0f, pullAccel);

        damagePerTick = Mathf.Max(0f, dmgPerTick);
        tickInterval = Mathf.Max(0.01f, tickIntv);

        centerOffset = aoeCenterOffset;
        maxHits = Mathf.Max(4, maxHitCount);

        if (_overlap == null || _overlap.Length != maxHits)
            _overlap = new Collider[maxHits];

        transform.rotation = Quaternion.LookRotation(_dir, Vector3.up);
    }

    private void Awake()
    {
        _overlap = new Collider[Mathf.Max(4, maxHits)];
        if (visualRoot == null) visualRoot = transform; // 최소 안전장치
    }

    private void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;

        float step = speed * dt;
        transform.position += _dir * step;
        _traveled += step;

        float remain = maxDistance - _traveled;
        if (remain <= 0f)
        {
            Destroy(gameObject);
            return;
        }

        float scale = 1f;
        if (remain <= shrinkStartDistance)
            scale = Mathf.Clamp01(remain / shrinkStartDistance);

        ApplyScale(scale);

        float pullRadius = basePullRadius * scale;
        float dmgRadius = baseDamageRadius * scale;

        if (pullRadius <= 0.01f && dmgRadius <= 0.01f)
            return;

        DoPullAndDot(dt, pullRadius, dmgRadius);
    }

    private void ApplyScale(float s)
    {
        if (!visualRoot) return;
        visualRoot.localScale = new Vector3(s, s, s);
    }

    private void DoPullAndDot(float dt, float pullRadius, float dmgRadius)
    {
        _pulledThisFrame.Clear();

        Vector3 center = transform.position + centerOffset;

        int count = Physics.OverlapSphereNonAlloc(
            center,
            pullRadius,
            _overlap,
            targetMask,
            QueryTriggerInteraction.Collide
        );

        Debug.Log($"[Whirlwind] count = {count}");

        float now = Time.time;

        for (int i = 0; i < count; i++)
        {
            Debug.Log(
                $"Hit: {_overlap[i].name}, Layer={LayerMask.LayerToName(_overlap[i].gameObject.layer)}"
            );
            Collider col = _overlap[i];
            if (!col) continue;

            Rigidbody rb = col.attachedRigidbody;
            if (!rb) continue;

            Transform root = rb.transform.root;
            int id = rb.GetInstanceID();

            if (!_pulledThisFrame.Contains(id))
            {
                _pulledThisFrame.Add(id);
                PullRigidbody(rb, center, pullRadius, dt);
            }

            Vector3 toCenter = center - rb.worldCenterOfMass;
            toCenter.y = 0f;
            float dist = toCenter.magnitude;

            if (dist <= dmgRadius)
            {
                if (!_nextTickAt.TryGetValue(id, out float next) || now >= next)
                {
                    _nextTickAt[id] = now + tickInterval;

                    var dmg = col.GetComponentInParent<IAoeDamageable>();
                    if (dmg != null)
                    {
                        dmg.ApplyAoeDamage(
                            damagePerTick,
                            _owner != null ? _owner : transform
                        );
                    }
                }
            }
        }
    }

    private void PullRigidbody(Rigidbody rb, Vector3 center, float radius, float dt)
    {
        Vector3 toCenter = center - rb.worldCenterOfMass;
        toCenter.y = 0f;

        float dist = toCenter.magnitude;
        if (dist <= 0.001f || radius <= 0.001f) return;

        float t = Mathf.Clamp01(1f - (dist / radius)); // 가까울수록 1
        float desiredSpeed = pullMaxSpeed * t;

        Vector3 dir = toCenter / dist;
        Vector3 desiredVel = dir * desiredSpeed;

        if (rb.isKinematic)
        {
            rb.MovePosition(rb.position + desiredVel * dt);
        }
        else
        {
            Vector3 currentVel = rb.velocity;
            Vector3 planarVel = new Vector3(currentVel.x, 0f, currentVel.z);

            // 목표 속도까지 가속
            Vector3 accel = (desiredVel - planarVel) / Mathf.Max(dt, 0.0001f);
            accel = Vector3.ClampMagnitude(accel, pullAcceleration);

            rb.AddForce(accel, ForceMode.Acceleration);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 center = transform.position + centerOffset;
        Gizmos.DrawWireSphere(center, basePullRadius);
        Gizmos.DrawWireSphere(center, baseDamageRadius);
    }
}
