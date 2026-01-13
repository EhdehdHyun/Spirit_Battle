using UnityEngine;

public class UIVisibilityManager : MonoBehaviour
{
    public static UIVisibilityManager Instance { get; private set; }

    [Header("GameOver 때 숨길 UI 루트들 (GameOverUI 제외)")]
    [SerializeField] private GameObject[] uiRootsToHide;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void HideAllExceptGameOver()
    {
        if (uiRootsToHide == null) return;

        foreach (var go in uiRootsToHide)
        {
            if (go != null) go.SetActive(false);
        }
    }

    public void RestoreAll()
    {
        if (uiRootsToHide == null) return;

        foreach (var go in uiRootsToHide)
        {
            if (go != null) go.SetActive(true);
        }
    }
}
