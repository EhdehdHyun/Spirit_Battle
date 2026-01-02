using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
public class GroundSlash : MonoBehaviour
{
    [Header("Move")]
    public float speed = 30f;
    public float lifeTime = 2f;

    [Header("Hit")]
    public LayerMask hitMask;

    private Transform owner;
    private Transform ownerRoot;
    private Vector3 dir;
    private float t;

    private float finalDamage;

    private readonly HashSet<int> hitIds = new HashSet<int>();

    private void Awake()
    {
        var rb = GetComponent<Rigidbody>();
        var col = GetComponent<Collider>();
        col.isTrigger = true;
        rb.useGravity = false;
        rb.isKinematic = true;
    }

    public void Fire(Transform owner, Vector3 forward, float finalDamage)
    {
        this.owner = owner;
        this.ownerRoot = owner ? owner.root : null;

        dir = forward.sqrMagnitude > 0.001f ? forward.normalized : (owner ? owner.forward : transform.forward);
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.0001f) dir.Normalize();

        this.finalDamage = finalDamage;

        t = 0f;
        hitIds.Clear();
        transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
    }

    private void Update()
    {
        t += Time.deltaTime;
        if (t >= lifeTime)
        {
            Destroy(gameObject);
            return;
        }

        transform.position += dir * speed * Time.deltaTime;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (ownerRoot != null && other.transform.root == ownerRoot) return;

        // 레이어 필터
        if ((hitMask.value & (1 << other.gameObject.layer)) == 0) return;

        var target = other.GetComponentInParent<IDamageable>();
        if (target == null || !target.IsAlive) return;

        // 같은 적 1번만(관통 + 1회 hit)
        int id = (target as Component).GetInstanceID();
        if (!hitIds.Add(id)) return;

        Vector3 hitPoint = other.ClosestPoint(transform.position);
        Vector3 hitNormal = (other.bounds.center - transform.position).normalized;

        target.TakeDamage(new DamageInfo(finalDamage, hitPoint, hitNormal));
    }
}
