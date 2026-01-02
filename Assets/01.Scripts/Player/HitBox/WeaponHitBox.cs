using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public interface IDamageableFeedback : IDamageable
{
    //실제 데미지 적용 판단
    bool TryTakeDamage(DamageInfo info);
}
public class WeaponHitBox : MonoBehaviour
{
    [Header("대상 레이어")]
    [SerializeField] private LayerMask targetLayers;

    [Header("패링 스턴시간")]
    [SerializeField] private float _parryStunTime = 1.5f;

    // Active 동안 데미지 1회라도 성공 하면 1번 호출
    public event Action DamageAppliedOnce;

    private Collider _col;
    private bool _active = false;
    private float _currentDamage;
    private HashSet<IDamageable> _alreadyHit = new HashSet<IDamageable>();
    private Transform _ownerRoot;
    private bool _firedThisActivation = false;

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
        _firedThisActivation = false; 
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

        var info = new DamageInfo(_currentDamage, hitPoint, hitNormal);

        bool applied;
        if(damageable is IDamageableFeedback fb)
        {
            applied = fb.TryTakeDamage(info); 
        }
        else
        {
            damageable.TakeDamage(info);
            applied = true;
        }

        if (!applied) return;

        _alreadyHit.Add(damageable);

        if (!_firedThisActivation)
        {
            _firedThisActivation = true;
            DamageAppliedOnce?.Invoke();
        }
    }


}
