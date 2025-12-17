using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponHitBox : MonoBehaviour
{
    [Header("대상 레이어")]
    [SerializeField] private LayerMask targetLayers;

    [Header("패링 스턴시간")]
    [SerializeField] private float _parryStunTime = 1.5f;

    private Collider _col;
    private bool _active = false;
    private float _currentDamage;
    private HashSet<IDamageable> _alreadyHit = new HashSet<IDamageable>();
    private Transform _ownerRoot;

    // 외부 조회용 프로퍼티
    public Transform OwnerRoot => _ownerRoot;
    public float ParryStunTime => _parryStunTime;

    private void Awake()
    {
        _col = GetComponent<Collider>();
        _col.isTrigger = true;
        _col.enabled = false;
        _ownerRoot = transform.root;
    }

    public void Activate(float finalDamage)
    {
        _active = true;
        _currentDamage = finalDamage;
        _alreadyHit.Clear();
        _col.enabled = true;
    }

    public void DeActivate()
    {
        _active = false;
        _col.enabled = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!_active) return;

        if (_ownerRoot != null && other.transform.root == _ownerRoot)
            return;

        if ((targetLayers.value & (1 << other.gameObject.layer)) == 0)
            return;

        var damageable = other.GetComponentInParent<IDamageable>();
        if (damageable == null) return;

        if (!damageable.IsAlive) return;
        if (_alreadyHit.Contains(damageable)) return;

        Vector3 hitPoint = other.ClosestPoint(transform.position);
        Vector3 hitNormal = (other.bounds.center - transform.position).normalized;

        damageable.TakeDamage(new DamageInfo(_currentDamage, hitPoint, hitNormal));
        _alreadyHit.Add(damageable);
    }


}
