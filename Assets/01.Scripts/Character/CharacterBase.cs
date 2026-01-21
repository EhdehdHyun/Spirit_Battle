using System;
using UnityEngine;

public abstract class CharacterBase : MonoBehaviour, IDamageable, IAoeDamageable
{
    [Header("공통 스탯")]
    public float maxHp = 100f;
    public float currentHp;
    public float moveSpeed = 3f;

    // 외부(UI 등)에서 사망 소식을 듣기 위한 이벤트
    public event Action<DamageInfo> OnDied;
    public event Action<float, float> OnHpChanged;

    protected bool isDead = false;
    public bool IsDead => isDead;

    private float _invincibleUntil = -1f;
    public bool IsInvincible => Time.time < _invincibleUntil;

    [Header("피격 모션 기준")]
    [SerializeField] private float heavyHit = 25f;

    protected float LastFinalDamage { get; private set; }
    protected bool LastHeavyHit { get; private set; }

    public void StartInvincible(float duration)
    {
        if (duration <= 0f) return;
        _invincibleUntil = Mathf.Max(_invincibleUntil, Time.time + duration);
    }

    public bool IsAlive => currentHp > 0f;

    protected virtual void Awake()
    {
        if (currentHp <= 0f) currentHp = maxHp;
        isDead = false;
    }

    private void OnEnable()
    {
        Debug.Log($"[CHAR Enable] {name} HP={currentHp} isDead={isDead}");
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

    public void ResetCharacter()
    {
        isDead = false;
        currentHp = maxHp;
        OnHpChanged?.Invoke(currentHp, maxHp);
    }

    public void TakeDamage(DamageInfo info)
    {
        if (isDead || IsInvincible) return;

        float multiplier = Mathf.Max(0f, GetIncomingDamageMultiplier(info));
        float finalDamage = info.amount * multiplier;

        currentHp -= finalDamage;

        LastFinalDamage = finalDamage;
        LastHeavyHit = finalDamage >= heavyHit;

        if (currentHp <= 0)
        {
            if (isDead) return;

            // 일반적인 피격 사망 처리
            isDead = true;
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

    public void ForceKill(DamageInfo info)
    {
        if (isDead) return;

        isDead = true;
        currentHp = 0;

        OnHpChanged?.Invoke(currentHp, maxHp);

        OnDie(info);
        OnDied?.Invoke(info);
    }

    public void ForceKill()
    {
        ForceKill(new DamageInfo());
    }

    public void ApplyAoeDamage(float damage, Transform attacker)
    {
        DamageInfo info = MakeAoeDamageInfo(damage, attacker);
        TakeDamage(info);
    }

    protected virtual DamageInfo MakeAoeDamageInfo(float damage, Transform attacker)
    {
        return new DamageInfo { amount = damage };
    }

    protected virtual void OnDamaged(DamageInfo info) { }
    public virtual void OnDie(DamageInfo info)
    {
    }

    public virtual void OnDie()
    {
        OnDie(new DamageInfo());
    }
}