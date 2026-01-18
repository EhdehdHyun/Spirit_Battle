using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-100)]
public class QuestTargetRegistry : MonoBehaviour
{
    public static QuestTargetRegistry Instance;

    private Dictionary<int, List<Transform>> targets = new();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }
    public Transform GetAnyTarget(int id)
    {
        if (!targets.ContainsKey(id))
            return null;

        if (targets[id].Count == 0)
            return null;

        return targets[id][0];
    }

    public void Register(int id, Transform t)
    {
        Debug.Log($"[Registry] Register id={id} name={t.name}");
        if (!targets.ContainsKey(id))
            targets[id] = new List<Transform>();

        if (!targets[id].Contains(t))
            targets[id].Add(t);
    }

    public void Unregister(int id, Transform t)
    {
        if (!targets.ContainsKey(id)) return;

        targets[id].Remove(t);

        if (targets[id].Count == 0)
            targets.Remove(id);
    }

    public Transform GetClosestTarget(int id, Vector3 from)
    {
        if (!targets.ContainsKey(id))
        {
            Debug.Log($"[Registry] No key for id={id}");
            return null;
        }

        Debug.Log($"[Registry] id={id} count={targets[id].Count}");

        Transform closest = null;
        float minDist = float.MaxValue;

        foreach (var t in targets[id])
        {
            if (t == null)
            {
                Debug.Log("[Registry] target is null");
                continue;
            }

            Debug.Log($"[Registry] check {t.name} active={t.gameObject.activeInHierarchy}");

            float d = Vector3.Distance(from, t.position);
            if (d < minDist)
            {
                minDist = d;
                closest = t;
            }
        }

        Debug.Log($"[Registry] closest={(closest ? closest.name : "NULL")}");
        return closest;
    }
}