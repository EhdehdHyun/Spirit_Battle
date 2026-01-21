using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShrineQuestTarget : MonoBehaviour
{
    [Header("Quest Target ID")]
    [SerializeField] private int shrineTargetId;

    private void OnEnable()
    {
        if (shrineTargetId <= 0) return;

        QuestTargetRegistry.Instance?.Register(shrineTargetId, transform);
    }

    private void OnDisable()
    {
        if (shrineTargetId <= 0) return;

        QuestTargetRegistry.Instance?.Unregister(shrineTargetId, transform);
    }
}

