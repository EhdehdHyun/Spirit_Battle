using System.Collections;
using UnityEngine;

public class BossSpawnInteratableOnce : MonoBehaviour, IInteractable
{
    [Header("Prompt")]
    [SerializeField] private string prompt = "Press [F]";

    [Header("Boss Root (비활성화로 씬에 배치)")]
    [SerializeField] private GameObject bossRoot;
    [SerializeField] private BossEnemy boss;

    [Header("등장 연출 시간")]
    [SerializeField] private float spawnDelay = 0f;
    [Header("플레이어 텔레포트")]
    [SerializeField] private bool teleportPlayerOnUse = true;
    [SerializeField] private Transform teleportTarget;

    [Tooltip("텔레포트 후 플레이어 회전도 맞출지")]
    [SerializeField] private bool matchRotation = true;

    [Header("옵션")]
    [SerializeField] private bool linkBossUIOnSpawn = true;
    [SerializeField] private bool disableColliderOnUse = true;

    private bool used = false;
    private Coroutine co;
    private Collider col;

    private void Awake()
    {
        col = GetComponent<Collider>();
    }


    public string GetInteractPrompt()
    {
        //이미 사용했으면 프롬프트 안 뜨게
        if (used) return string.Empty;
        return prompt;
    }

    public void Interact(PlayerInteraction player)
    {
        if (used) return;
        if (co != null) return;

        used = true;

        // 다시는 레이캐스트에 안 걸리게 즉 다시 누를려고 할 때 상호작용 불가
        if (disableColliderOnUse && col != null)
            col.enabled = false;

        if (teleportPlayerOnUse && player != null && teleportTarget != null)
            TeleportPlayer(player.transform, teleportTarget.position, teleportTarget.rotation);

        co = StartCoroutine(SpawnRoutine());
    }

    private IEnumerator SpawnRoutine()
    {
        if (spawnDelay > 0f)
            yield return new WaitForSeconds(spawnDelay);

        if (bossRoot != null)
            bossRoot.SetActive(true);

        // boss 참조 없으면 bossRoot에서 찾아오기
        if (boss == null && bossRoot != null)
            boss = bossRoot.GetComponentInChildren<BossEnemy>(true);

        // 보스 UI 연결(선택)
        if (linkBossUIOnSpawn && boss != null && BossUIStatus.Instance != null)
            BossUIStatus.Instance.SetBoss(boss);

        co = null;
    }

    private void TeleportPlayer(Transform playerTf, Vector3 pos, Quaternion rot)
    {
        // 1) CharacterController 쓰는 경우: disable 했다가 위치 변경 후 enable
        var cc = playerTf.GetComponent<CharacterController>();
        if (cc != null)
        {
            cc.enabled = false;
            playerTf.position = pos;
            if (matchRotation) playerTf.rotation = rot;
            cc.enabled = true;
            return;
        }

        // 2) Rigidbody 쓰는 경우: MovePosition/velocity 초기화
        var rb = playerTf.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            rb.position = pos;
            if (matchRotation) rb.rotation = rot;
            return;
        }

        // 3) 그 외: 그냥 Transform 이동
        playerTf.position = pos;
        if (matchRotation) playerTf.rotation = rot;
    }
}
