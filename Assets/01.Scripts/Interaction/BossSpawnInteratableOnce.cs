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
    [Tooltip("전체 화면을 덮는 로딩 패널/캔버스 오브젝트 (SetActive로 on/off)")]
    [SerializeField] private GameObject loadingOverlay;
    [Tooltip("로딩이 너무 번쩍이지 않게 최소 유지 시간(초)")]
    [SerializeField] private float loadingMinDuration = 0.25f;

    private bool used = false;
    private Coroutine co;
    private Collider col;

    private void Awake()
    {
        col = GetComponent<Collider>();
        if (loadingOverlay != null)
            loadingOverlay.SetActive(false);
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

        // 다시는 상호작용 안되게
        if (disableColliderOnUse && col != null)
            col.enabled = false;

        co = StartCoroutine(UseRoutine(player));
    }

    private IEnumerator UseRoutine(PlayerInteraction player)
    {
        // SFX 끄기(원하는 방식 아무거나)
        DisableSfx();

        // 로딩 오버레이 켜기(텔포 장면 가리기)
        float loadingStart = Time.unscaledTime;
        if (loadingOverlay != null)
            loadingOverlay.SetActive(true);

        // 텔레포트
        if (teleportPlayerOnUse && player != null && teleportTarget != null)
            TeleportPlayer(player.transform, teleportTarget.position, teleportTarget.rotation);

        // 텔포 직후 카메라/추적 스냅을 한 프레임 더 가리기
        yield return null;

        // overlay가 먼저 화면에 그려지게 한 프레임 양보
        yield return null;

        if (loadingMinDuration > 0f)
            yield return new WaitForSecondsRealtime(loadingMinDuration);

        if (loadingOverlay != null)
            loadingOverlay.SetActive(false);

        // 스폰 딜레이(연출 시간)
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
