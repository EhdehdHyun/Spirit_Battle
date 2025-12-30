using UnityEngine;

public class TutorialBossMarker : MonoBehaviour
{
    public static int ActiveCount { get; private set; }

    private void OnEnable() => ActiveCount++;
    private void OnDisable() => ActiveCount = Mathf.Max(0, ActiveCount - 1);
}
