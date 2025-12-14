using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponHitBox : MonoBehaviour
{
    [Header("대상 레이어")]
    [SerializeField] private LayerMask targetLayers;

    private Collider _col;
    private bool _active = false;
    private float _currentDamage;
    private HashSet<IDamageable> _alreadyHit = new HashSet<IDamageable>();
    private Transform _ownerRoot;

    private void Awake()
    {
        _col = GetComponent<Collider>();
        _col.isTrigger = true;
        //공격 타이밍에만 켜기
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
        if(!_active) return;

        if (_ownerRoot != null && other.transform.root == _ownerRoot)
            return;
        // 레이어 저장을 효율적으로 하기위해 비트로 변경 O(1)
        if ((targetLayers.value & (1 << other.gameObject.layer)) == 0)
            return;
        
        if(!other.TryGetComponent<IDamageable>(out var damageable))
        {
            damageable = other.GetComponentInParent<IDamageable>();
            if (damageable == null) return;
        }

        if(!damageable.IsAlive) return;
        if (_alreadyHit.Contains(damageable)) return;

        Vector3 hitPoint = other.ClosestPoint(transform.position);
        Vector3 dir = (other.bounds.center - transform.position).normalized;
        Vector3 hitNormal = dir;

        DamageInfo info = new DamageInfo(_currentDamage, hitPoint, hitNormal);
        damageable.TakeDamage(info);

        _alreadyHit.Add(damageable);
    }
}
