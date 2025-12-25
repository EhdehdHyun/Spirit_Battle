using System;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Rendering;

// 플레이어/몬스터 공통 사용 기본 클래스
// 체력, 이동속도, 피격/사망 로직 기본형 제공

public abstract class CharacterBase : MonoBehaviour, IDamageable
{
    [Header("공통 스탯")]
    public float maxHp = 100f;
    public float currentHp;
    public float moveSpeed = 3f;
    public event Action<DamageInfo> OnDied;

    public event Action<float, float> OnHpChanged;

    //무적
    private float _invincibleUntil = -1f;
    public bool IsInvincible => Time.time < _invincibleUntil;


    public void StartInvincible(float duration)
    {
        if (duration <= 0f) return;
        _invincibleUntil = Mathf.Max(_invincibleUntil, Time.time + duration);
    }

    public bool IsAlive => currentHp > 0f;

    protected virtual void Awake()
    {
        currentHp = maxHp;
    }

    protected virtual float GetIncomingDamageMultiplier(DamageInfo info) => 1f;

    public void SetHp(float newCurrentHp, bool notify = true)
    {
        currentHp = Mathf.Clamp(newCurrentHp, 0f, maxHp);
        if (notify) OnHpChanged?.Invoke(currentHp, maxHp);
    }

    public void RestoreFullHp(bool notify = true)
    {
        SetHp(maxHp, notify);
    }

    //데미지 받았을 때 호출
    public void TakeDamage(DamageInfo info)
    {
        Debug.Log($"[DMG] TakeDamage called amount={info.amount} inv={IsInvincible}", this);

        if (!IsAlive) return;

        if (IsInvincible)
        {
            return;
        }

        float multiplier = Mathf.Max(0f, GetIncomingDamageMultiplier(info));
        float finalDamage = info.amount * multiplier;

        currentHp -= finalDamage;

        if (currentHp <= 0)
        {
            currentHp = 0;
            OnHpChanged?.Invoke(currentHp, maxHp);
            OnDie(info);
            OnDied?.Invoke(info);
        }
        else
        {
            OnHpChanged?.Invoke(currentHp, maxHp);
            OnDamaged(info);
        }
    }

    protected virtual void OnDamaged(DamageInfo info) { }
    protected virtual void OnDie(DamageInfo info) { }

}
