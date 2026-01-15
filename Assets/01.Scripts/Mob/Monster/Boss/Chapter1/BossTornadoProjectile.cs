using System.Collections.Generic;
using UnityEngine;

public class BossTornadoProjectile : MonoBehaviour
{
    [Header("Move (Rigidbody)")]
    [SerializeField] private float speed = 10f;
    [SerializeField] private float travelDistance = 6f;

    [Header("Hit")]
    [SerializeField] private LayerMask hitMask;
    [SerializeField] private float damage = 25f;

    [Tooltip("같은 대상은 한 번만 맞게")]
    [SerializeField] private bool hitOncePerTarget = true;

    [Tooltip("첫 타격 시 즉시 파괴할지")]
    [SerializeField] private bool destroyOnFirstHit = false;

    [Header("Optional")]
    [SerializeField] private Transform visualRoot;

    private Rigidbody _rb;
    private Transform _owner;
    private Vector3 _dir;
    private float _traveled;

    private readonly HashSet<int> _hitIds = new HashSet<int>();

    public void Init(Transform owner, Vector3 forward, float spd, float dist, float dmg, LayerMask mask)
    {
        _owner = owner;

        forward.y = 0f;
        _dir = forward.sqrMagnitude > 0.0001f ? forward.normalized : transform.forward;

        speed = Mathf.Max(0f, spd);
        travelDistance = Mathf.Max(0.01f, dist);
        damage = Mathf.Max(0f, dmg);
        hitMask = mask;

        transform.rotation = Quaternion.LookRotation(_dir, Vector3.up);
    }

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        if (_rb == null)
        {
            _rb = gameObject.AddComponent<Rigidbody>();
        }

        _rb.useGravity = false;
        _rb.isKinematic = true;
        _rb.interpolation = RigidbodyInterpolation.Interpolate;

        if (visualRoot == null) visualRoot = transform;
    }

    private void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;
        float step = speed * dt;

        Vector3 next = _rb.position + _dir * step;
        _rb.MovePosition(next);

        _traveled += step;
        if (_traveled >= travelDistance)
            Destroy(gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (((1 << other.gameObject.layer) & hitMask.value) == 0) return;

        if (_owner != null && other.transform.IsChildOf(_owner)) return;

        // 피해 대상 찾기
        var dmgable = other.GetComponentInParent<IDamageable>();
        if (dmgable == null) return;

        int id = dmgable.GetHashCode(); // 간단키
        if (hitOncePerTarget)
        {
            if (_hitIds.Contains(id)) return;
            _hitIds.Add(id);
        }

        Vector3 hitPoint = other.ClosestPoint(transform.position);
        Vector3 hitNormal = (hitPoint - transform.position).normalized;

        DamageInfo info = new DamageInfo(damage, hitPoint, hitNormal);
        dmgable.TakeDamage(info);

        if (destroyOnFirstHit)
            Destroy(gameObject);
    }
}
