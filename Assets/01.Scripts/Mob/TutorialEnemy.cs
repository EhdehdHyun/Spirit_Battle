using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TutorialEnemy : MonoBehaviour
{
    [SerializeField] private AttackAreaBlocker blocker;
    [SerializeField] private WorldArrowController worldArrow;

    private bool handled;

    public void OnTutorialEnemyDead()
    {
        if (handled) return;
        handled = true;

        blocker?.Open();
        worldArrow?.ClearTarget();
    }
}
