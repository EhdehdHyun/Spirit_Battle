using UnityEngine;

public static class GlobalInputBlocker
{
    // 로딩,연출 중 키 입력 막기용
    public static bool BlockKeyboard { get; private set; }

    public static void SetKeyboardBlocked(bool blocked)
    {
        BlockKeyboard = blocked;
    }
}