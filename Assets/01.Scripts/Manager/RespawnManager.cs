using System.Collections;
using UnityEngine;

public class RespawnManager : MonoBehaviour
{
    [Header("Respawn Point")]
    [SerializeField] private Transform respawnPoint;

    [Header("Player Root GameObject (비워두면 Tag=Player로 탐색)")]
    [SerializeField] private GameObject playerRoot;

    [Header("Tutorial Settings")]
    [SerializeField] private bool isFirstDeath = true;

    public Transform CurrentRespawnPoint => respawnPoint;

    private static readonly int HashWeaponEquipped = Animator.StringToHash("WeaponEquipped");
    private static readonly int HashRespawn = Animator.StringToHash("Respawn");
    private static readonly int HashIdle = Animator.StringToHash("Idle");

    private IEnumerator Start()
    {
        if (playerRoot == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) playerRoot = p;
        }

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
    }

    public void Retry()
    {
        if (playerRoot == null || respawnPoint == null)
        {
            Debug.LogWarning("[RespawnManager] playerRoot 또는 respawnPoint 없음");
            return;
        }

        UIVisibilityManager.Instance?.RestoreAll();

        // 이동(텔레포트)
        TeleportPlayerRoot(playerRoot, respawnPoint.position);

        // HP 풀회복 + 입력,이동락 해제
        var baseChar = playerRoot.GetComponentInParent<CharacterBase>();
        if (baseChar != null)
            baseChar.RestoreFullHp();

        var input = playerRoot.GetComponentInParent<PlayerInputController>();
        input?.Unlock();

        var phy = playerRoot.GetComponentInParent<PhysicsCharacter>();
        if (phy != null)
            phy.SetMovementLocked(false);

        if (isFirstDeath)
        {
            isFirstDeath = false;

            if (GameManager.Instance != null)
            {
                Debug.Log("[RespawnManager] 첫 부활 성공! 저장 기능 해금.");
                GameManager.Instance.CompleteTutorial();
            }
        }

        var animator = playerRoot.GetComponentInChildren<Animator>();
        ResetAnimatorForRespawn(animator);

        if (GameOverUI.Instance != null)
            GameOverUI.Instance.Hide();

        GlobalInputBlocker.SetKeyBlocked(KeyCode.Tab, false);
        GlobalInputBlocker.SetKeyBlocked(KeyCode.M, false);

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void TeleportPlayerRoot(GameObject root, Vector3 pos)
    {
        // Rigidbody가 있으면 rb.position으로 이동
        var rb = root.GetComponent<Rigidbody>()
              ?? root.GetComponentInChildren<Rigidbody>()
              ?? root.GetComponentInParent<Rigidbody>();

        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.position = pos;
            root.transform.position = pos;
        }
        else
        {
            root.transform.position = pos;
        }
    }

    private void ResetAnimatorForRespawn(Animator animator)
    {
        if (animator == null) return;

        animator.SetTrigger(HashRespawn);
    }
}