using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

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

    [Header("Retry UI")]
    [SerializeField] private GameObject retryRoot;
    [SerializeField] private Button retryButton;

    [Header("Forced Dialogue UI (튜토보스 전용)")]
    [SerializeField] private GameObject tutorialPanelRoot;
    [SerializeField] private TMP_Text tutorialBodyText;
    [SerializeField] private TMP_Text tutorialHintText;
    [SerializeField] private string tutorialHint = "아무 키나 눌러 계속";

    [Header("옵션")]
    [SerializeField] private bool pauseTimeAfterFade = true;
    [SerializeField] private bool showCursorOnGameOver = true;

    private bool showing;
    private bool waitingAnyKey;
    private bool fading;

    // ✅ 전용 대사 모드 상태
    private bool dialogueMode;
    private string[] dialogueLines = Array.Empty<string>();
    private int dialogueIndex;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (retryButton != null)
            retryButton.onClick.AddListener(() => OnRetryPressed?.Invoke());

        HideImmediate();
    }

    private void Update()
    {
        if (!showing) return;
        if (!waitingAnyKey) return;

        if (!IsAnyKeyPressedThisFrame()) return;

        if (dialogueMode)
        {
            AdvanceDialogueOrShowRetry();
        }
        else
        {
            waitingAnyKey = false;
            SetRetryVisible(true);
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
        fading = true;
        waitingAnyKey = false;

        dialogueMode = useDialogue;
        dialogueLines = lines ?? Array.Empty<string>();
        dialogueIndex = 0;

        gameObject.SetActive(true);

        rootGroup.alpha = 0f;
        rootGroup.blocksRaycasts = true;
        rootGroup.interactable = true;

        if (titleText != null)
        {
            titleText.text = string.IsNullOrEmpty(title) ? defaultTitle : title;
            titleText.gameObject.SetActive(false);
        }

        SetRetryVisible(false);
        SetTutorialPanelVisible(false);

        if (showCursorOnGameOver)
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }

        StopAllCoroutines();
        StartCoroutine(FadeInThenPauseRoutine());
    }

    private IEnumerator FadeInThenPauseRoutine()
    {
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime;
            rootGroup.alpha = Mathf.Clamp01(t / Mathf.Max(0.0001f, fadeDuration));
            yield return null;
        }

        rootGroup.alpha = 1f;

        // 타이틀 "확" 등장
        if (titleText != null)
            titleText.gameObject.SetActive(true);

        // 시간 멈춤
        if (pauseTimeAfterFade)
            Time.timeScale = 0f;

        fading = false;

        // ✅ 여기서부터 "아무 키" 입력을 받기 시작
        waitingAnyKey = true;

        // ✅ 전용 대사 모드면: 패널/텍스트 켜고 1줄 보여주기
        if (dialogueMode)
        {
            if (dialogueLines.Length > 0)
            {
                SetTutorialPanelVisible(true);
                RefreshDialogue();
            }
            else
            {
                // 라인이 없으면 그냥 일반처럼 처리
                dialogueMode = false;
            }
        }
    }

    private void AdvanceDialogueOrShowRetry()
    {
        if (dialogueLines == null || dialogueLines.Length == 0)
        {
            dialogueMode = false;
            waitingAnyKey = false;
            SetRetryVisible(true);
            return;
        }

        dialogueIndex++;

        if (dialogueIndex >= dialogueLines.Length)
        {
            // ✅ 대사 끝: 패널/텍스트 끄고 리스폰 버튼 보여주기
            dialogueMode = false;
            waitingAnyKey = false;

            SetTutorialPanelVisible(false);
            SetRetryVisible(true);
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

    private void SetRetryVisible(bool visible)
    {
        if (retryRoot != null) retryRoot.SetActive(visible);
        else if (retryButton != null) retryButton.gameObject.SetActive(visible);
    }

    private void SetTutorialPanelVisible(bool visible)
    {
        if (tutorialPanelRoot != null) tutorialPanelRoot.SetActive(visible);
    }

    public void Hide()
    {
        showing = false;
        waitingAnyKey = false;
        fading = false;
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
        SetRetryVisible(false);
    }
}
