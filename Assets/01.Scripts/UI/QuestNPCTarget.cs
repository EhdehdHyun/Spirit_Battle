using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QuestNpcTarget : MonoBehaviour
{
    [SerializeField] private int npcId;

    
    private void Awake()
    {
        TryRegister();
    }
    private void OnEnable()
    {
        TryRegister();
    }

    private void OnDisable()
    {
        if (npcId > 0 && QuestTargetRegistry.Instance != null)
        {
            QuestTargetRegistry.Instance.Unregister(npcId, transform);
        }
    }
    private void TryRegister()
    {
        if (npcId > 0 && QuestTargetRegistry.Instance != null)
        {
            QuestTargetRegistry.Instance.Register(npcId, transform);
        }
    }
}

