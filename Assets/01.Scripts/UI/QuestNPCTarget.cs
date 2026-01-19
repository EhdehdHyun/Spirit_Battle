using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QuestNpcTarget : MonoBehaviour
{
    [SerializeField] private int npcId;

    private void OnEnable()
    {
        if (npcId > 0 && QuestTargetRegistry.Instance != null)
        {
            QuestTargetRegistry.Instance.Register(npcId, transform);
            Debug.Log($"[Registry] NPC Target Register {npcId}");
        }
    }

    private void OnDisable()
    {
        if (npcId > 0 && QuestTargetRegistry.Instance != null)
        {
            QuestTargetRegistry.Instance.Unregister(npcId, transform);
        }
    }
}

