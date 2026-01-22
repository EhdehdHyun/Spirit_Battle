using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class BossSpawnInteratableOnce : MonoBehaviour, IInteractable
{
    [Header("Boss Type")]
    [Tooltip("체크하면 '튜토리얼 보스' 규칙 적용(재입장 X)")]
    [SerializeField] private bool isTutorialBossPortal = false;

    [Header("Prompt")]
    [SerializeField] private string prompt = "Press [F]";

    [Header("Boss Template Root (비활성화로 씬에 배치, 일반 보스는 Instantiate 템플릿로 사용)")]
    [SerializeField] private GameObject bossRoot;

    [Header("Optional: Spawn Override (비워두면 bossRoot의 초기 위치/회전 사용)")]
    [SerializeField] private Transform bossSpawnOverride;

    [Header("등장 연출 시간")]
    [SerializeField] private float spawnDelay = 0f;

    [Header("플레이어 텔레포트")]
    [SerializeField] private bool teleportPlayerOnUse = true;
    [SerializeField] private Transform teleportTarget;
    [SerializeField] private bool matchRotation = true;

    [Header("옵션")]
    [SerializeField] private bool linkBossUIOnSpawn = true;
    [SerializeField] private bool disableColliderDuringSession = true;

    [Header("퀘스트 ID HUD")]
    [SerializeField] private int shrineHudTargetId = 50002;

    [Header("SFX (한 번 쓰면 끄기)")]
    [SerializeField] private GameObject sfxObjectToDisable;
    [SerializeField] private AudioSource sfxAudioSourceToStop;

    [Header("Loading Overlay (텔포 순간 가리기)")]
    [SerializeField] private GameObject loadingOverlay;
    [SerializeField] private float loadingMinDuration = 0.25f;

    [Header("Respawn Switch (보스맵 진입 시 리스폰 지점 변경)")]
    [SerializeField] private bool changeRespawnPointOnUse = true;
    [SerializeField] private Transform bossRespawnPoint;
    [SerializeField] private RespawnManager respawnManager;

    [Header("After Clear Return (일반 보스)")]
    [SerializeField] private float returnDelayAfterClear = 3f;
    [SerializeField] private bool returnPlayerToPortal = true;

    [Header("Player refs (optional)")]
    [SerializeField] private PlayerInputController inputController;
    [SerializeField] private PlayerInput playerInput;

    private Collider _col;
    private Coroutine _co;

    private bool _usedTutorial = false;
    private bool _inBossSession = false;
    private bool _bossDiedHandled = false;

    private Vector3 _bossTemplatePos;
    private Quaternion _bossTemplateRot;

    private Transform _playerTf;
    private CharacterBase _playerChar;

    private GameObject _bossInstance;
    private BossEnemy _boss;

    private void Awake()
    {
        _col = GetComponent<Collider>();

        if (loadingOverlay != null)
            loadingOverlay.SetActive(false);

        if (respawnManager == null)
            respawnManager = FindObjectOfType<RespawnManager>();

        if (bossRoot != null)
        {
            _bossTemplatePos = bossRoot.transform.position;
            _bossTemplateRot = bossRoot.transform.rotation;

            // 템플릿은 항상 비활성 유지(일반 보스는 Instantiate로만 사용)
            if (!isTutorialBossPortal)
                bossRoot.SetActive(false);
        }
    }

    private void OnDisable()
    {
        UnsubscribePlayerDied();
        UnsubscribeBossDied();
    }

    public string GetInteractPrompt()
    {
        if (isTutorialBossPortal && _usedTutorial) return string.Empty;
        return prompt;
    }

    public void Interact(PlayerInteraction player)
    {
        if (_co != null) return;
        if (isTutorialBossPortal && _usedTutorial) return;

        // 성소 HUD 제거 (기존 유지)
        if (shrineHudTargetId > 0)
        {
            QuestTargetRegistry.Instance?.Unregister(shrineHudTargetId, transform);
            QuestHUDUI.Instance?.ClearCurrentTarget();
        }

        _co = StartCoroutine(CoStartSession(player));
    }

    private IEnumerator CoStartSession(PlayerInteraction player)
    {
        CachePlayer(player);
        SubscribePlayerDied();

        if (disableColliderDuringSession && _col != null)
            _col.enabled = false;

        // 보스맵 리스폰 포인트로 교체
        if (changeRespawnPointOnUse && respawnManager != null)
        {
            Transform newRespawn = bossRespawnPoint != null ? bossRespawnPoint : teleportTarget;
            if (newRespawn != null)
                respawnManager.SetRespawnPoint(newRespawn);
        }

        DisableSfx();

        // 로딩/입력 블락 + 텔포
        yield return StartCoroutine(CoTeleportIn());

        if (spawnDelay > 0f)
            yield return new WaitForSeconds(spawnDelay);

        SpawnBoss();

        SubscribeBossDied();

        // UI 링크
        if (linkBossUIOnSpawn && _boss != null && BossUIStatus.Instance != null)
            BossUIStatus.Instance.SetBoss(_boss);

        _inBossSession = true;
        _bossDiedHandled = false;

        if (isTutorialBossPortal)
            _usedTutorial = true;

        _co = null;
    }

    private IEnumerator CoTeleportIn()
    {
        if (loadingOverlay != null)
        {
            loadingOverlay.SetActive(true);
            UIVisibilityManager.Instance?.HideAllExceptGameOver();
            GlobalInputBlocker.SetKeyboardBlocked(true, allowEsc: false);
        }

        if (teleportPlayerOnUse && _playerTf != null && teleportTarget != null)
        {
            TeleportPlayer(_playerTf, teleportTarget.position, teleportTarget.rotation);
        }

        // 프레임 2번 양보(텔포/물리 안정화)
        yield return null;
        yield return null;

        if (loadingMinDuration > 0f)
            yield return new WaitForSecondsRealtime(loadingMinDuration);

        if (loadingOverlay != null)
        {
            loadingOverlay.SetActive(false);
            UIVisibilityManager.Instance?.RestoreAll();
            GlobalInputBlocker.SetKeyboardBlocked(false, allowEsc: true);
        }
    }

    private void SpawnBoss()
    {
        CleanupBossInstance(); // 혹시 남아있으면 정리

        if (bossRoot == null) return;

        Vector3 spawnPos = bossSpawnOverride ? bossSpawnOverride.position : _bossTemplatePos;
        Quaternion spawnRot = bossSpawnOverride ? bossSpawnOverride.rotation : _bossTemplateRot;

        if (isTutorialBossPortal)
        {
            // 튜토리얼 보스는 "재입장 X"이므로 템플릿 자체를 켜도 됨
            _bossInstance = bossRoot;
            _bossInstance.transform.SetPositionAndRotation(spawnPos, spawnRot);
            _bossInstance.SetActive(true);
        }
        else
        {
            // 일반 보스는 매 세션 Instantiate(재입장/재시도 안정성 확보)
            _bossInstance = Instantiate(bossRoot, spawnPos, spawnRot);
            _bossInstance.name = bossRoot.name + "_Instance";
            _bossInstance.SetActive(true);
        }

        _boss = _bossInstance.GetComponentInChildren<BossEnemy>(true);

        if (_boss != null)
        {
            // 일반 보스는 재도전용 리셋
            if (!isTutorialBossPortal)
                _boss.ResetForRetry();

            // 세션 타겟 명시 초기화
            _boss.InitializeForSession(_playerTf);
        }
    }

    private void CleanupBossInstance()
    {
        if (_boss != null)
        {
            UnsubscribeBossDied();
            _boss = null;
        }

        if (_bossInstance != null)
        {
            if (isTutorialBossPortal)
            {
                // 튜토 보스는 템플릿 오브젝트라 Destroy 금지
                _bossInstance.SetActive(false);
            }
            else
            {
                Destroy(_bossInstance);
            }
            _bossInstance = null;
        }
    }

    private void CachePlayer(PlayerInteraction player)
    {
        if (player == null) return;

        _playerTf = player.transform;
        if (_playerChar == null)
            _playerChar = _playerTf.GetComponentInParent<CharacterBase>();

        if (inputController == null)
            inputController = _playerTf.GetComponentInParent<PlayerInputController>();

        if (playerInput == null)
            playerInput = _playerTf.GetComponentInParent<PlayerInput>();
    }

    private void SubscribePlayerDied()
    {
        if (_playerChar == null) return;
        UnsubscribePlayerDied();
        _playerChar.OnDied += OnPlayerDied;
    }

    private void UnsubscribePlayerDied()
    {
        if (_playerChar == null) return;
        _playerChar.OnDied -= OnPlayerDied;
    }

    private void SubscribeBossDied()
    {
        if (_boss == null) return;
        UnsubscribeBossDied();
        _boss.OnDied += OnBossDied;
    }

    private void UnsubscribeBossDied()
    {
        if (_boss == null) return;
        _boss.OnDied -= OnBossDied;
    }

    private void OnPlayerDied(DamageInfo info)
    {
        if (!_inBossSession) return;

        _inBossSession = false;

        BossUIStatus.Instance?.SetVisible(false);

        UnsubscribeBossDied();
        UnsubscribePlayerDied();

        CleanupBossInstance();

        // 튜토 보스는 포탈도 끝
        if (isTutorialBossPortal)
        {
            gameObject.SetActive(false);
            return;
        }

        // 일반 보스는 재입장 가능 -> 포탈 다시 열기
        if (disableColliderDuringSession && _col != null)
            _col.enabled = true;
    }

    private void OnBossDied(DamageInfo info)
    {
        if (_bossDiedHandled) return;
        _bossDiedHandled = true;

        if (!_inBossSession) return;

        _inBossSession = false;

        UnsubscribeBossDied();
        UnsubscribePlayerDied();

        if (isTutorialBossPortal)
        {
            // 튜토 보스는 그냥 종료(재입장 X는 usedTutorial로 막힘)
            CleanupBossInstance();
            return;
        }

        StartCoroutine(CoReturnAfterClear());
    }

    private IEnumerator CoReturnAfterClear()
    {
        yield return new WaitForSecondsRealtime(Mathf.Max(0f, returnDelayAfterClear));

        if (loadingOverlay != null)
        {
            loadingOverlay.SetActive(true);
            UIVisibilityManager.Instance?.HideAllExceptGameOver();
            GlobalInputBlocker.SetKeyboardBlocked(true, allowEsc: false);
        }

        yield return null;
        yield return null;

        if (returnPlayerToPortal && _playerTf != null)
        {
            Quaternion rot = matchRotation ? transform.rotation : _playerTf.rotation;
            TeleportPlayer(_playerTf, transform.position, rot);
        }

        if (loadingMinDuration > 0f)
            yield return new WaitForSecondsRealtime(loadingMinDuration);

        if (loadingOverlay != null)
        {
            loadingOverlay.SetActive(false);
            UIVisibilityManager.Instance?.RestoreAll();
            GlobalInputBlocker.SetKeyboardBlocked(false, allowEsc: true);
        }

        CleanupBossInstance();

        // 일반 보스는 "클리어 후 포탈 닫기"
        gameObject.SetActive(false);
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

            playerTf.GetComponentInParent<PlayerFallDamage>()?.ResetTracking();
            return;
        }

        var rb = playerTf.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            rb.position = pos;
            if (matchRotation) rb.rotation = rot;

            playerTf.GetComponentInParent<PlayerFallDamage>()?.ResetTracking();
            return;
        }

        playerTf.position = pos;
        if (matchRotation) playerTf.rotation = rot;

        playerTf.GetComponentInParent<PlayerFallDamage>()?.ResetTracking();
    }
}
