using System.Collections.Generic;
using UnityEngine;

public class QuestTargetRegistry : MonoBehaviour
{
    public static QuestTargetRegistry Instance;

    private Dictionary<int, Transform> targets = new();

    private void Awake()
    {
        Instance = this;
    }

    public void Register(int id, Transform target)
    {
        if (!targets.ContainsKey(id))
            targets.Add(id, target);
    }

    public void Unregister(int id)
    {
        if (targets.ContainsKey(id))
            targets.Remove(id);
    }

    public Transform GetTargetTransform(int id)
    {
        targets.TryGetValue(id, out var t);
        return t;
    }
}