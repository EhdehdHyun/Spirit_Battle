using System.Collections;
using UnityEngine;

public class RespawnManager : MonoBehaviour
{
    [Header("Respawn Point")]
    [SerializeField] private Transform respawnPoint;

    [Header("Player Root GameObject (비워두면 Tag=Player로 탐색)")]
    [SerializeField] private GameObject playerRoot;

    public Transform CurrentRespawnPoint => respawnPoint;

    private IEnumerator Start()
    {
        if (playerRoot == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) playerRoot = p;
        }

        // GameOverUI.Instance가 준비될 때까지 기다렸다가 구독
        while (GameOverUI.Instance == null) yield return null;
        GameOverUI.Instance.OnRetryPressed += Retry;
    }

    private void OnDestroy()
    {
        if (GameOverUI.Instance != null)
            GameOverUI.Instance.OnRetryPressed -= Retry;
    }

    // 보스맵 진입 등 특정 타이밍에 리스폰 지점 변경
    public void SetRespawnPoint(Transform newPoint)
    {
        if (newPoint == null)
        {
            Debug.LogWarning("[RespawnManager] SetRespawnPoint: newPoint가 null입니다.");
            return;
        }

        respawnPoint = newPoint;
        // Debug.Log($"[RespawnManager] RespawnPoint changed => {newPoint.name}");
    }

    public void Retry()
    {
        if (playerRoot == null || respawnPoint == null)
        {
            Debug.LogWarning("[RespawnManager] playerRoot 또는 respawnPoint 없음");
            return;
        }

        // 보스 HP바 등 끄기
        if (BossUIStatus.Instance != null)
        {
            BossUIStatus.Instance.SetVisible(false);
        }

        // 이동(텔레포트)
        TeleportPlayerRoot(playerRoot, respawnPoint.position);

        // HP 풀회복 + 입력/이동락 해제
        var baseChar = playerRoot.GetComponentInParent<CharacterBase>();
        if (baseChar != null)
            baseChar.RestoreFullHp();

        var input = playerRoot.GetComponentInParent<PlayerInputController>();
        input?.Unlock();

        var phy = playerRoot.GetComponentInParent<PhysicsCharacter>();
        if (phy != null)
            phy.SetMovementLocked(false);

        // 애니메이션 “처음으로”
        var animator = playerRoot.GetComponentInChildren<Animator>();
        if (animator != null)
        {
            animator.Rebind();
            animator.Update(0f);
        }

        // 게임오버 UI 닫기(시간 복구는 GameOverUI.Hide가 한다고 가정)
        if (GameOverUI.Instance != null)
            GameOverUI.Instance.Hide();

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void TeleportPlayerRoot(GameObject root, Vector3 pos)
    {
        // Rigidbody가 있으면 rb.position으로 이동 (스냅백 방지)
        var rb = root.GetComponent<Rigidbody>()
              ?? root.GetComponentInChildren<Rigidbody>()
              ?? root.GetComponentInParent<Rigidbody>();

        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.position = pos;
            root.transform.position = pos; // 동기화(안전)
        }
        else
        {
            root.transform.position = pos;
        }
    }
}
