using System;

public static class MonsterKillEvent
{
    public static Action<int> OnMonsterKilled;

    public static void Raise(int monsterId)
    {
        OnMonsterKilled?.Invoke(monsterId);
    }
}