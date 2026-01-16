using System.Collections;
using UnityEngine.InputSystem;
using UnityEngine;

public class BossSpawnInteratableOnce : MonoBehaviour, IInteractable
{
    [Header("Boss Type")]
    [Tooltip("체크하면 '튜토리얼 보스' 규칙 적용(재입장 X, 플레이어 사망 시 포탈+보스 OFF 등)")]
    [SerializeField] private bool isTutorialBossPortal = false;

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

    [Header("Respawn Switch (보스맵 진입 시 리스폰 지점 변경)")]
    [SerializeField] private bool changeRespawnPointOnUse = true;
    [Tooltip("보스맵에서 리스폰될 위치(=B). 비워두면 teleportTarget을 사용")]
    [SerializeField] private Transform bossRespawnPoint;
    [SerializeField] private RespawnManager respawnManager;

    [Header("After Clear Return (일반 보스만)")]
    [Tooltip("일반 보스 클리어 시, n초 후 로딩 오버레이 띄우고 포탈 위치로 복귀 텔포")]
    [SerializeField] private float returnDelayAfterClear = 3f;
    [Tooltip("클리어 후 복귀 텔포를 포탈 오브젝트 위치로 할지")]

    [SerializeField] private PlayerInputController inputController;
    [SerializeField] private PlayerInput playerInput;
    [SerializeField] private bool returnPlayerToPortal = true;

    private bool usedTutorial = false;
    private bool inBossSession = false;
    private Coroutine co;
    private Collider col;

    private Vector3 bossInitPos;
    private Quaternion bossInitRot;

    private CharacterBase playerChar;
    private Transform playerTf;

    private bool bossDiedHandled = false;

    private void Awake()
    {
        col = GetComponent<Collider>();

        if (loadingOverlay != null)
            loadingOverlay.SetActive(false);

        if (respawnManager == null)
            respawnManager = FindObjectOfType<RespawnManager>();

        CacheBossRefs();

        if (bossRoot != null)
        {
            bossInitPos = bossRoot.transform.position;
            bossInitRot = bossRoot.transform.rotation;
        }
    }

    private void OnEnable()
    {
        CacheBossRefs();
    }

    private void OnDisable()
    {
        UnsubscribePlayerDied();
        UnsubscribeBossDied();
    }

    private void CacheBossRefs()
    {
        if (boss == null && bossRoot != null)
            boss = bossRoot.GetComponentInChildren<BossEnemy>(true);
    }


    public string GetInteractPrompt()
    {
        if (isTutorialBossPortal && usedTutorial) return string.Empty;
        return prompt;
    }

    public void Interact(PlayerInteraction player)
    {
        if (co != null) return;

        if (isTutorialBossPortal && usedTutorial)
            return;

        if (disableColliderOnUse && col != null)
            col.enabled = false;

        co = StartCoroutine(UseRoutine(player));
    }

    private IEnumerator UseRoutine(PlayerInteraction player)
    {
        CacheBossRefs();

        // 플레이어 캐싱 & 죽음 이벤트 구독
        CachePlayer(player);
        SubscribePlayerDied();

        // 보스맵 리스폰 포인트로 교체
        if (changeRespawnPointOnUse && respawnManager != null)
        {
            Transform newRespawn = bossRespawnPoint != null ? bossRespawnPoint : teleportTarget;
            if (newRespawn != null)
                respawnManager.SetRespawnPoint(newRespawn);
        }

        // SFX 끄기
        DisableSfx();

        if (loadingOverlay != null)
        {
            loadingOverlay.SetActive(true);
            GlobalInputBlocker.SetKeyboardBlocked(true);
        }

        if (teleportPlayerOnUse && player != null && teleportTarget != null)
            TeleportPlayer(player.transform, teleportTarget.position, teleportTarget.rotation);

        yield return null;
        yield return null;

        if (loadingMinDuration > 0f)
            yield return new WaitForSecondsRealtime(loadingMinDuration);

        if (loadingOverlay != null)
        {
            loadingOverlay.SetActive(false);
            GlobalInputBlocker.SetKeyboardBlocked(false);
        }

        if (spawnDelay > 0f)
            yield return new WaitForSeconds(spawnDelay);

        if (!isTutorialBossPortal)
        {
            ResetBossForRetry();
        }

        // 보스 활성화
        if (bossRoot != null)
            bossRoot.SetActive(true);

        CacheBossRefs();

        SubscribeBossDied();

        // UI 링크
        if (linkBossUIOnSpawn && boss != null && BossUIStatus.Instance != null)
            BossUIStatus.Instance.SetBoss(boss);

        inBossSession = true;
        bossDiedHandled = false;

        if (isTutorialBossPortal)
        {
            usedTutorial = true;

            gameObject.SetActive(false);
        }

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

    private void ResetBossForRetry()
    {
        if (bossRoot == null) return;

        if (bossRoot.activeSelf)
            bossRoot.SetActive(false);

        bossRoot.transform.SetPositionAndRotation(bossInitPos, bossInitRot);

        CacheBossRefs();

        if (boss != null)
        {
            boss.RestoreFullHp(true);

            boss.ResetForRetry_IfExists();
        }
    }

    private void CachePlayer(PlayerInteraction player)
    {
        if (player == null) return;
        playerTf = player.transform;
        if (playerChar == null)
            playerChar = playerTf.GetComponentInParent<CharacterBase>();

        if (inputController == null)
            inputController = playerTf.GetComponentInParent<PlayerInputController>();

        if (playerInput == null)
            playerInput = playerTf.GetComponentInParent<PlayerInput>();
    }

    private void SubscribePlayerDied()
    {
        if (playerChar == null) return;
        UnsubscribePlayerDied();
        playerChar.OnDied += OnPlayerDied;
    }

    private void UnsubscribePlayerDied()
    {
        if (playerChar == null) return;
        playerChar.OnDied -= OnPlayerDied;
    }

    private void SubscribeBossDied()
    {
        if (boss == null) return;
        UnsubscribeBossDied();
        boss.OnDied += OnBossDied;
    }

    private void UnsubscribeBossDied()
    {
        if (boss == null) return;
        boss.OnDied -= OnBossDied;
    }

    private void OnPlayerDied(DamageInfo info)
    {
        if (!inBossSession) return;

        if (bossRoot != null)
            bossRoot.SetActive(false);

        if (isTutorialBossPortal)
        {
            gameObject.SetActive(false);
        }

        inBossSession = false;

        UnsubscribeBossDied();
        UnsubscribePlayerDied();
    }

    private void OnBossDied(DamageInfo info)
    {
        if (bossDiedHandled) return;
        bossDiedHandled = true;

        if (!inBossSession) return;

        if (isTutorialBossPortal)
        {
            inBossSession = false;
            UnsubscribeBossDied();
            UnsubscribePlayerDied();
            return;
        }

        StartCoroutine(CoReturnAfterClear());
    }

    private IEnumerator CoReturnAfterClear()
    {
        // 세션 종료
        inBossSession = false;

        yield return new WaitForSecondsRealtime(Mathf.Max(0f, returnDelayAfterClear));

        if (loadingOverlay != null)
        {
            loadingOverlay.SetActive(true);
            GlobalInputBlocker.SetKeyboardBlocked(true);
        }

        // 프레임 양보
        yield return null;
        yield return null;

        // 포탈 위치로 복귀
        if (returnPlayerToPortal && playerTf != null)
        {
            Quaternion rot = matchRotation ? transform.rotation : playerTf.rotation;
            TeleportPlayer(playerTf, transform.position, rot);
        }

        if (loadingMinDuration > 0f)
            yield return new WaitForSecondsRealtime(loadingMinDuration);

        if (loadingOverlay != null)
        {
            loadingOverlay.SetActive(false);
            GlobalInputBlocker.SetKeyboardBlocked(false);
        }

        // 포탈 닫기(일반 보스는 “클리어 후” 비활성화)
        gameObject.SetActive(false);

        UnsubscribeBossDied();
        UnsubscribePlayerDied();
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

public static class BossEnemyRetryExtensions
{
    public static void ResetForRetry_IfExists(this BossEnemy boss)
    {
        if (boss == null) return;

        boss.SendMessage("ResetForRetry", SendMessageOptions.DontRequireReceiver);
    }
}
