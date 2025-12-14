using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossCore : MonoBehaviour
{
    [Header("코어 내구도")]
    public float maxHp = 50f;

    private float currentHp;
    private BossAIController bossAI;

    public bool IsAlive => currentHp > 0f;

    private void Awake()
    {
        currentHp = maxHp;
        bossAI = GetComponentInParent<BossAIController>();

        // 기본적으로는 BossEnemy에서 Phase2 진입 시 활성화하지만 안전하게 시작 시에도 비활성 처리
        gameObject.SetActive(false);
    }

    public void TakeDamage(DamageInfo info)
    {
        if (!IsAlive) return;

        currentHp -= info.amount;

        if (currentHp <= 0f)
        {
            currentHp = 0f;
            OnCoreBroken(info);
        }
    }

    private void OnCoreBroken(DamageInfo info)
    {
        Debug.Log($"{name} : 코어 파괴!");

        // 코어 비활성
        gameObject.SetActive(false);

        // 보스 Down 상태 진입 요청
        if (bossAI != null)
        {
            bossAI.HandleCoreBroken();
        }
    }
}
