using System.Collections.Generic;
using UnityEngine;

public static class GlobalInputBlocker
{
    // 로딩,연출 중 키 입력 막기용 (모든 키보드 입력 차단)
    public static bool BlockAllKeyboard { get; private set; }

    // esc 키만 예외적으로 허용
    public static bool AllowEscWhileBlocked { get; private set; }

    // 특정 키만
    private static readonly HashSet<KeyCode> blockedKeys = new HashSet<KeyCode>();

    public static void SetKeyBlocked(KeyCode key, bool blocked)
    {
        if (blocked) blockedKeys.Add(key);
        else blockedKeys.Remove(key);
    }

    //모든 키 막는 메써드
    public static void SetKeyboardBlocked(bool blocked, bool allowEsc = false)
    {
        BlockAllKeyboard = blocked;
        AllowEscWhileBlocked = allowEsc;
    }

    public static void ClearBlockedKeys()
    {
        blockedKeys.Clear();
    }

    public static bool IsKeyBlocked(KeyCode key)
    {
        if (BlockAllKeyboard)
        {
            if (AllowEscWhileBlocked && key == KeyCode.Escape) return false;
            return true;
        }

        return blockedKeys.Contains(key);
    }
}