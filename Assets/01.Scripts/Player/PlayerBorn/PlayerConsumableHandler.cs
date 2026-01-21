using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class PlayerConsumableHandler : MonoBehaviour
{
    [Serializable]
    public class HealItem
    {
        public int itemKey;      // 예: Apple의 data.key
        public int healAmount;   // 예: 20
    }

    [Header("회복 아이템 설정")]
    public List<HealItem> healItems = new List<HealItem>();

    // 캐시(힐 메서드 찾기)
    private object _healTarget;
    private MethodInfo _healMethod;
    private Type _healParamType;

    private void Awake()
    {
        CacheHealMethod();
    }
    public bool TryApplyConsumable(Data_table data, int amount)
    {
        if (data == null || amount <= 0) return false;

        int healPerOne = 0;
        for (int i = 0; i < healItems.Count; i++)
        {
            if (healItems[i].itemKey == data.key)
            {
                healPerOne = healItems[i].healAmount;
                break;
            }
        }

        if (healPerOne <= 0)
            return false;

        int totalHeal = healPerOne * amount;
        if (_healMethod == null || _healTarget == null)
            CacheHealMethod();

        if (_healMethod == null || _healTarget == null)
        {
            return false;
        }

        object ret;
        if (_healParamType == typeof(int))
            ret = _healMethod.Invoke(_healTarget, new object[] { totalHeal });
        else
            ret = _healMethod.Invoke(_healTarget, new object[] { (float)totalHeal });
        if (ret is bool b) return b;
        return true;
    }

    private void CacheHealMethod()
    {
        _healTarget = null;
        _healMethod = null;
        _healParamType = null;

        // 플레이어에 붙은 컴포넌트들에서 "힐" 메서드 자동 탐색
        var comps = GetComponents<MonoBehaviour>();
        string[] candidates = { "TryHeal", "Heal", "AddHp", "RecoverHp", "RestoreHp", "RestoreHP" };

        foreach (var c in comps)
        {
            if (c == null) continue;
            var t = c.GetType();

            foreach (var name in candidates)
            {
                // int 파라미터
                var mInt = t.GetMethod(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                    null, new[] { typeof(int) }, null);
                if (mInt != null)
                {
                    _healTarget = c;
                    _healMethod = mInt;
                    _healParamType = typeof(int);
                    return;
                }

                // float 파라미터
                var mFloat = t.GetMethod(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                    null, new[] { typeof(float) }, null);
                if (mFloat != null)
                {
                    _healTarget = c;
                    _healMethod = mFloat;
                    _healParamType = typeof(float);
                    return;
                }
            }
        }
    }
}
