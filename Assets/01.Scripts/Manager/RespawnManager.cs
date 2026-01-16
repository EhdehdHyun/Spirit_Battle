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

        UIVisibilityManager.Instance?.RestoreAll();

        // 이동(텔레포트)
        TeleportPlayerRoot(playerRoot, respawnPoint.position);

        // if (isFirstDeath)
        // {
        //     //인벤토리 비우기
        //     if (InventoryManager.Instance != null)
        //     {
        //         // 0번 슬롯 = EquipWeaponIndex
        //         InventoryManager.Instance.ClearSlot(InventoryManager.EquipWeaponIndex);
        //         Debug.Log("[RespawnManager] 튜토리얼 강제 사망: 무기가 제거되었습니다.");
        //     }

        //     QuestManager.Instance.AcceptQuest(40000);
        //     isFirstDeath = false;
        // }


        // HP 풀회복 + 입력,이동락 해제
        var baseChar = playerRoot.GetComponentInParent<CharacterBase>();
        if (baseChar != null)
            baseChar.RestoreFullHp();

        var input = playerRoot.GetComponentInParent<PlayerInputController>();
        input?.Unlock();

        var phy = playerRoot.GetComponentInParent<PhysicsCharacter>();
        if (phy != null)
            phy.SetMovementLocked(false);

        // 애니메이션 “처음으로”
        // var animator = playerRoot.GetComponentInChildren<Animator>();
        // if (animator != null)
        // {
        //     animator.Rebind();
        //     animator.Update(0f);

        //     ResetAnimatorForRespawn(animator);
        // }
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

        // foreach (var p in animator.parameters)
        // {
        //     if (p.type == AnimatorControllerParameterType.Trigger)
        //         animator.ResetTrigger(p.name);
        // }

        // animator.SetBool(HashWeaponEquipped, false);

        animator.SetTrigger(HashRespawn);
    }
}