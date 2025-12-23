using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum AbilityId
{
    AirDash,
    Skill1,
}
public class PlayerAbility : MonoBehaviour
{
    [SerializeField] private List<AbilityId> defaultUnlocked = new(); // 시작부터 열린 것
    private HashSet<AbilityId> unlocked;

    void Awake()
    {
        unlocked = new HashSet<AbilityId>(defaultUnlocked);
    }

    public bool Has(AbilityId id) => unlocked.Contains(id);
    public void Unlock(AbilityId id) => unlocked.Add(id);
}
