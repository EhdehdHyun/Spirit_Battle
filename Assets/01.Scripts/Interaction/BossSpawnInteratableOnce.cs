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

    [Header("SFX (한 번 쓰면 끄기)")]
    [Tooltip("이 오브젝트 자체를 꺼버림(가장 확실)")]
    [SerializeField] private GameObject sfxObjectToDisable;
    [Tooltip("오브젝트는 못 끄는 경우 AudioSource만 Stop하고 disable")]
    [SerializeField] private AudioSource sfxAudioSourceToStop;

    [Header("Loading Overlay (텔포 순간 가리기)")]
    [SerializeField] private GameObject loadingOverlay;
    [SerializeField] private float loadingMinDuration = 0.25f;

    // 추가: 보스맵 진입 시 리스폰 포인트 변경
    [Header("Respawn Switch (보스맵 진입 시 리스폰 지점 변경)")]
    [SerializeField] private bool changeRespawnPointOnUse = true;
    [Tooltip("보스맵에서 리스폰될 위치(=B). 비워두면 teleportTarget을 사용")]
    [SerializeField] private Transform bossRespawnPoint;
    [SerializeField] private RespawnManager respawnManager;

    private bool used = false;
    private Coroutine co;
    private Collider col;

    private void Awake()
    {
        col = GetComponent<Collider>();

        if (loadingOverlay != null)
            loadingOverlay.SetActive(false);

        if (respawnManager == null)
            respawnManager = FindObjectOfType<RespawnManager>();
    }

    public string GetInteractPrompt()
    {
        if (used) return string.Empty;
        return prompt;
    }

    public void Interact(PlayerInteraction player)
    {
        if (used) return;
        if (co != null) return;

        used = true;

        if (disableColliderOnUse && col != null)
            col.enabled = false;

        co = StartCoroutine(UseRoutine(player));
    }

    private IEnumerator UseRoutine(PlayerInteraction player)
    {
        // 보스맵 리스폰 포인트로 교체 (이 시점부터 앞으로 B에서 부활)
        if (changeRespawnPointOnUse && respawnManager != null)
        {
            Transform newRespawn = bossRespawnPoint != null ? bossRespawnPoint : teleportTarget;
            if (newRespawn != null)
                respawnManager.SetRespawnPoint(newRespawn);
        }

        // SFX 끄기
        DisableSfx();

        // 로딩 오버레이 켜기
        if (loadingOverlay != null)
            loadingOverlay.SetActive(true);

        // 텔레포트
        if (teleportPlayerOnUse && player != null && teleportTarget != null)
            TeleportPlayer(player.transform, teleportTarget.position, teleportTarget.rotation);

        // 카메라 스냅 가리기용 프레임 양보
        yield return null;
        yield return null;

        // 로딩 최소 유지시간만큼만 켜기
        if (loadingMinDuration > 0f)
            yield return new WaitForSecondsRealtime(loadingMinDuration);

        if (loadingOverlay != null)
            loadingOverlay.SetActive(false);

        // 스폰 딜레이(연출)
        if (spawnDelay > 0f)
            yield return new WaitForSeconds(spawnDelay);

        // 보스 활성화
        if (bossRoot != null)
            bossRoot.SetActive(true);

        if (boss == null && bossRoot != null)
            boss = bossRoot.GetComponentInChildren<BossEnemy>(true);

        if (linkBossUIOnSpawn && boss != null && BossUIStatus.Instance != null)
            BossUIStatus.Instance.SetBoss(boss);

        co = null;
    }

    private void DisableSfx()
    {
        if (sfxObjectToDisable != null)
        {
            sfxObjectToDisable.SetActive(false);
            return;
        }

        if (sfxAudioSourceToStop != null)
        {
            sfxAudioSourceToStop.Stop();
            sfxAudioSourceToStop.enabled = false;
        }
    }

    private void TeleportPlayer(Transform playerTf, Vector3 pos, Quaternion rot)
    {
        var cc = playerTf.GetComponent<CharacterController>();
        if (cc != null)
        {
            cc.enabled = false;
            playerTf.position = pos;
            if (matchRotation) playerTf.rotation = rot;
            cc.enabled = true;
            return;
        }

        var rb = playerTf.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            rb.position = pos;
            if (matchRotation) rb.rotation = rot;
            return;
        }

        playerTf.position = pos;
        if (matchRotation) playerTf.rotation = rot;
    }
}
