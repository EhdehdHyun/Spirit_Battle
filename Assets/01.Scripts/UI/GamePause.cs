using System.Collections.Generic;
using UnityEngine;

public static class GamePause
{
    private static readonly HashSet<object> _owners = new();
    private static float _savedScale = 1f;

    public static bool IsPaused => _owners.Count > 0;

    public static void Request(object owner)
    {
        if (owner == null) return;

        if (_owners.Count == 0)
        {
            _savedScale = Time.timeScale;
            if (_savedScale <= 0f) _savedScale = 1f;
            Time.timeScale = 0f;
        }

        _owners.Add(owner);
    }

    public static void Release(object owner)
    {
        if (owner == null) return;

        _owners.Remove(owner);

        if (_owners.Count == 0)
        {
            Time.timeScale = _savedScale;
        }
    }
}
