using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TutorialEnemy : MonoBehaviour
{
    [SerializeField] private AttackAreaBlocker blocker;
    [SerializeField] private WorldArrowController worldArrow;
    
    [SerializeField] private Transform nextEnemyTarget;
    
    [TextArea]
    [SerializeField] private string onClearMessage;

    private bool handled;

    public void OnTutorialEnemyDead()
    {
        if (handled) return;
        handled = true;

        blocker?.Open();
        
        //  문구 출력 (있을 때만)
        if (!string.IsNullOrEmpty(onClearMessage))
        {
            TutorialManager.Instance.ShowSimpleMessage(onClearMessage);
        }
        // 다음 몬스터 방향 화살표
        if (worldArrow != null && nextEnemyTarget != null)
        {
            worldArrow.SetTarget(nextEnemyTarget);
        }
        else
        {
            //마지막 몬스터면 화살표 끄기
            worldArrow?.ClearTarget();
        }
    }
}
