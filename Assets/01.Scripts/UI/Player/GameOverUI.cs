using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class GameOverUI : MonoBehaviour
{
    public static GameOverUI Instance { get; private set; }
    public event Action OnRetryPressed;

    [Header("Root (Fade 대상)")]
    [SerializeField] private CanvasGroup rootGroup;

    [Tooltip("0 -> 1 까지 페이드 인 시간(초)")]
    [SerializeField] private float fadeDuration = 2f;

    [Header("Title")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private string defaultTitle = "YOU DIED";

    [Header("Respawn Hint")]
    [SerializeField] private TMP_Text respawnHintText;      // "아무 키나 눌러 부활" 표시용
    [SerializeField] private string respawnHint = "아무 키나 눌러 부활";

    [Header("Forced Dialogue UI (튜토보스 전용)")]
    [SerializeField] private GameObject tutorialPanelRoot;
    [SerializeField] private TMP_Text tutorialBodyText;
    [SerializeField] private TMP_Text tutorialHintText;
    [SerializeField] private string tutorialHint = "아무 키나 눌러 계속";

    [Header("옵션")]
    [SerializeField] private bool pauseTimeAfterFade = true;
    [SerializeField] private bool showCursorOnGameOver = true;

    [Tooltip("죽는 프레임에 눌려있던 입력이 곧바로 처리되는 걸 방지(초)")]
    [SerializeField] private float inputBlockSeconds = 0.2f;

    private enum Phase
    {
        Hidden,
        Fading,
        WaitFirstAnyKey,     // (일반) 타이틀 뜬 뒤 첫 입력 -> 힌트 표시
        Dialogue,            // (튜토) 대사 진행 중
        WaitRespawnAnyKey    // (일반/튜토 공통) 힌트 표시 중 -> 입력 시 부활
    }

    private Phase phase = Phase.Hidden;

    private bool showing;
    private float inputBlockUntil;

    private bool dialogueMode;
    private string[] dialogueLines = Array.Empty<string>();
    private int dialogueIndex;

    private Coroutine fadeCo;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        HideImmediate();
    }

    private void Update()
    {
        if (!showing) return;
        if (Time.unscaledTime < inputBlockUntil) return;

        if (phase != Phase.WaitFirstAnyKey && phase != Phase.Dialogue && phase != Phase.WaitRespawnAnyKey)
            return;

        if (!IsAnyKeyPressedThisFrame())
            return;

        switch (phase)
        {
            case Phase.WaitFirstAnyKey:
                // 첫 입력 -> 힌트 보여주기 (깜빡임 없음)
                ShowRespawnHint();
                phase = Phase.WaitRespawnAnyKey;
                inputBlockUntil = Time.unscaledTime + inputBlockSeconds;
                break;

            case Phase.Dialogue:
                AdvanceDialogueOrGoToRespawnHint();
                inputBlockUntil = Time.unscaledTime + inputBlockSeconds;
                break;

            case Phase.WaitRespawnAnyKey:
                // 입력 -> 부활
                TriggerRespawn();
                break;
        }
    }

    public void ShowDeath(string title = null)
    {
        ShowInternal(title, useDialogue: false, lines: null);
    }

    public void ShowTutorialDeath(string title, string[] lines)
    {
        ShowInternal(title, useDialogue: true, lines: lines);
    }

    private void ShowInternal(string title, bool useDialogue, string[] lines)
    {
        if (rootGroup == null)
        {
            Debug.LogWarning("[GameOverUI] rootGroup(CanvasGroup)가 비어있음");
            return;
        }

        showing = true;
        dialogueMode = useDialogue;
        dialogueLines = lines ?? Array.Empty<string>();
        dialogueIndex = 0;

        gameObject.SetActive(true);

        // 초기 UI 상태
        rootGroup.alpha = 0f;
        rootGroup.blocksRaycasts = true;
        rootGroup.interactable = true;

        if (titleText != null)
        {
            titleText.text = string.IsNullOrEmpty(title) ? defaultTitle : title;
            titleText.gameObject.SetActive(false);
        }

        SetTutorialPanelVisible(false);
        HideRespawnHint();

        if (showCursorOnGameOver)
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }

        inputBlockUntil = Time.unscaledTime + inputBlockSeconds;

        if (fadeCo != null) StopCoroutine(fadeCo);
        fadeCo = StartCoroutine(FadeInThenPauseRoutine());
    }

    private IEnumerator FadeInThenPauseRoutine()
    {
        phase = Phase.Fading;

        float t = 0f;
        float dur = Mathf.Max(0.0001f, fadeDuration);

        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            rootGroup.alpha = Mathf.Clamp01(t / dur);
            yield return null;
        }

        rootGroup.alpha = 1f;

        // 타이틀 "확"
        if (titleText != null)
            titleText.gameObject.SetActive(true);

        // 시간 멈춤
        if (pauseTimeAfterFade)
            Time.timeScale = 0f;

        // 이제 입력을 받는다
        if (dialogueMode && dialogueLines.Length > 0)
        {
            SetTutorialPanelVisible(true);
            RefreshDialogue();
            phase = Phase.Dialogue;
        }
        else
        {
            dialogueMode = false;
            phase = Phase.WaitFirstAnyKey; // (일반) 첫 입력 대기
        }

        inputBlockUntil = Time.unscaledTime + inputBlockSeconds;
    }

    private void AdvanceDialogueOrGoToRespawnHint()
    {
        if (!dialogueMode || dialogueLines == null || dialogueLines.Length == 0)
        {
            dialogueMode = false;
            phase = Phase.WaitFirstAnyKey;
            return;
        }

        dialogueIndex++;

        if (dialogueIndex >= dialogueLines.Length)
        {
            // 대사 끝 -> 패널 끄고 "부활 안내"로
            dialogueMode = false;
            SetTutorialPanelVisible(false);

            ShowRespawnHint();
            phase = Phase.WaitRespawnAnyKey;
            return;
        }

        RefreshDialogue();
    }

    private void RefreshDialogue()
    {
        if (tutorialBodyText != null)
            tutorialBodyText.text = dialogueLines[Mathf.Clamp(dialogueIndex, 0, dialogueLines.Length - 1)];

        if (tutorialHintText != null)
            tutorialHintText.text = tutorialHint;
    }

    private void TriggerRespawn()
    {
        if (!showing) return;
        showing = false;

        HideRespawnHint();

        // RespawnManager가 이 이벤트를 구독해서 부활 처리
        OnRetryPressed?.Invoke();
    }

    // ====== Hint (No Blink) ======

    private void ShowRespawnHint()
    {
        if (respawnHintText == null) return;

        respawnHintText.text = respawnHint;
        respawnHintText.enabled = true;
        respawnHintText.gameObject.SetActive(true);
    }

    private void HideRespawnHint()
    {
        if (respawnHintText != null)
            respawnHintText.gameObject.SetActive(false);
    }

    // ====== Utility ======

    private void SetTutorialPanelVisible(bool visible)
    {
        if (tutorialPanelRoot != null)
            tutorialPanelRoot.SetActive(visible);
    }

    private bool IsAnyKeyPressedThisFrame()
    {
        if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame)
            return true;

        if (Mouse.current != null)
        {
            if (Mouse.current.leftButton.wasPressedThisFrame) return true;
            if (Mouse.current.rightButton.wasPressedThisFrame) return true;
            if (Mouse.current.middleButton.wasPressedThisFrame) return true;
        }

        if (Gamepad.current != null)
        {
            var g = Gamepad.current;
            if (g.buttonSouth.wasPressedThisFrame) return true;
            if (g.buttonNorth.wasPressedThisFrame) return true;
            if (g.buttonWest.wasPressedThisFrame) return true;
            if (g.buttonEast.wasPressedThisFrame) return true;
            if (g.startButton.wasPressedThisFrame) return true;
            if (g.selectButton.wasPressedThisFrame) return true;
        }

        return false;
    }

    public void Hide()
    {
        showing = false;
        phase = Phase.Hidden;
        dialogueMode = false;
        dialogueLines = Array.Empty<string>();
        dialogueIndex = 0;

        if (pauseTimeAfterFade)
            Time.timeScale = 1f;

        HideImmediate();
    }

    private void HideImmediate()
    {
        if (rootGroup != null)
        {
            rootGroup.alpha = 0f;
            rootGroup.blocksRaycasts = false;
            rootGroup.interactable = false;
        }

        if (titleText != null)
            titleText.gameObject.SetActive(false);

        SetTutorialPanelVisible(false);
        HideRespawnHint();
    }
}
