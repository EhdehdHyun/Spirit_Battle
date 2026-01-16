using UnityEngine;

public static class GlobalInputBlocker
{
    // 로딩,연출 중 키 입력 막기용 (모든 키보드 입력 차단)
    public static bool BlockKeyboard { get; private set; }

    // esc 키만 예외적으로 허용
    public static bool AllowEscWhileBlocked { get; private set; }

    public static void SetKeyboardBlocked(bool blocked, bool allowEsc = false)
    {
        BlockKeyboard = blocked;
        AllowEscWhileBlocked = allowEsc;
    }

    public static bool IsKeyBlocked(KeyCode key)
    {
        if (!BlockKeyboard) return false;
        if (AllowEscWhileBlocked && key == KeyCode.Escape) return false;

        return true;
    }
}