using UnityEngine;

public class RespawnManager : MonoBehaviour
{
    public static RespawnManager Instance { get; private set; }

    [Header("Spawn Point (현재는 1개)")]
    [SerializeField] private Transform spawnPoint;

    [Header("Player (비우면 Tag=Player로 자동 찾음)")]
    [SerializeField] private PlayerCharacter player;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        // GameOverUI의 Retry 버튼 이벤트 연결
        if (GameOverUI.Instance != null)
            GameOverUI.Instance.OnRetryPressed += Respawn;
    }

    private void OnDestroy()
    {
        if (GameOverUI.Instance != null)
            GameOverUI.Instance.OnRetryPressed -= Respawn;
    }

    public void SetSpawnPoint(Transform newPoint)
    {
        spawnPoint = newPoint;
    }

    public void Respawn()
    {
        if (spawnPoint == null)
        {
            Debug.LogWarning("[RespawnManager] spawnPoint가 비어있음");
            return;
        }

        if (player == null)
        {
            var pObj = GameObject.FindGameObjectWithTag("Player");
            if (pObj != null) player = pObj.GetComponentInParent<PlayerCharacter>();
        }

        if (player == null)
        {
            Debug.LogWarning("[RespawnManager] PlayerCharacter를 찾지 못함");
            return;
        }

        // UI 닫고 시간 복구
        if (GameOverUI.Instance != null)
            GameOverUI.Instance.Hide();
        else
            Time.timeScale = 1f;

        // ✅ 실제 부활
        player.RespawnAt(spawnPoint.position, spawnPoint.rotation);
    }
}
