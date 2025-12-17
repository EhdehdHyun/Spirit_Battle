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

    public event Action<float, float> OnHpChanged;


    public bool IsAlive => currentHp > 0f;

    protected virtual void Awake()
    {
        currentHp = maxHp;
    }

    protected virtual float GetIncomingDamageMultiplier(DamageInfo info) => 1f;

    //데미지 받았을 때 호출
    public void TakeDamage(DamageInfo info)
    {
        if (!IsAlive) return;

        float multiplier = Mathf.Max(0f, GetIncomingDamageMultiplier(info));
        float finalDamage = info.amount * multiplier;

        currentHp -= info.amount;

        if (currentHp <= 0)
        {
            currentHp = 0;
            OnHpChanged?.Invoke(currentHp, maxHp);
            OnDie(info);
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
