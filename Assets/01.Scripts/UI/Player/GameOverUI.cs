using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class GameOverUI : MonoBehaviour
{
    public static GameOverUI Instance { get; private set; }

    public event Action OnRetryPressed; // ✅ 추가

    [Header("Root (Fade)")]
    [SerializeField] private CanvasGroup rootGroup;
    [SerializeField] private float fadeInDuration = 0.35f;

    [Header("Texts")]
    [SerializeField] private TMP_Text titleText;

    [Header("Tutorial Panel")]
    [SerializeField] private GameObject tutorialPanelRoot;
    [SerializeField] private TMP_Text bodyText;
    [SerializeField] private TMP_Text hintText;

    [Header("Retry UI")]
    [SerializeField] private GameObject retryRoot;  // 버튼/패널 묶음
    [SerializeField] private Button retryButton;

    [Header("Input (New Input System)")]
    [SerializeField] private InputActionReference nextAction;
    [SerializeField] private bool alsoAllowKeyboardF = false;

    [Header("Time")]
    [SerializeField] private bool pauseTimeScale = true;

    private bool showing;
    private bool isTutorial;
    private bool fading;

    private bool sequenceEnded; // ✅ 마지막까지 봤는지 (중복 Next 방지)
    private string[] lines = Array.Empty<string>();
    private int index;
    private Action onSequenceFinished;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (retryButton != null)
            retryButton.onClick.AddListener(() => OnRetryPressed?.Invoke());

        HideImmediate();
    }

    private void OnEnable()
    {
        if (nextAction != null) nextAction.action.Enable();
    }

    private void OnDisable()
    {
        if (nextAction != null) nextAction.action.Disable();
    }

    private void Update()
    {
        if (!showing || fading) return;
        if (!isTutorial) return;
        if (sequenceEnded) return;

        bool nextPressed = false;

        if (nextAction != null && nextAction.action != null)
            nextPressed = nextAction.action.WasPressedThisFrame();

        if (!nextPressed && alsoAllowKeyboardF)
            nextPressed = Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame;

        if (nextPressed)
            Next();
    }

    public void ShowNormalDeath(string title = "YOU DIED")
    {
        showing = true;
        isTutorial = false;
        sequenceEnded = true;      // 일반 사망은 텍스트 진행 없음
        onSequenceFinished = null;

        gameObject.SetActive(true);

        if (titleText != null) titleText.text = title;
        if (tutorialPanelRoot != null) tutorialPanelRoot.SetActive(false);

        // ✅ 일반 사망은 바로 Retry 보여줄지 선택
        SetRetryVisible(true);

        StartFadeIn();

        if (pauseTimeScale)
            Time.timeScale = 0f;
    }

    public void ShowTutorialSequence(string title, string[] tutorialLines, Action onFinished)
    {
        showing = true;
        isTutorial = true;
        sequenceEnded = false;
        onSequenceFinished = onFinished;

        lines = tutorialLines ?? Array.Empty<string>();
        index = 0;

        gameObject.SetActive(true);

        if (titleText != null) titleText.text = title;
        if (tutorialPanelRoot != null) tutorialPanelRoot.SetActive(true);
        if (hintText != null) hintText.text = "Press [F]";

        // ✅ 진행 중엔 Retry 숨김
        SetRetryVisible(false);

        RefreshBody();
        StartFadeIn();

        if (pauseTimeScale)
            Time.timeScale = 0f;
    }

    private void SetRetryVisible(bool visible)
    {
        if (retryRoot != null) retryRoot.SetActive(visible);
        else if (retryButton != null) retryButton.gameObject.SetActive(visible);
    }

    private void StartFadeIn()
    {
        if (rootGroup == null) return;

        StopAllCoroutines();
        rootGroup.alpha = 0f;
        rootGroup.blocksRaycasts = true;
        rootGroup.interactable = true;

        StartCoroutine(FadeInRoutine());
    }

    private IEnumerator FadeInRoutine()
    {
        fading = true;

        float t = 0f;
        while (t < fadeInDuration)
        {
            t += Time.unscaledDeltaTime;
            rootGroup.alpha = Mathf.Clamp01(t / fadeInDuration);
            yield return null;
        }

        rootGroup.alpha = 1f;
        fading = false;
    }

    private void RefreshBody()
    {
        if (bodyText == null) return;

        if (lines == null || lines.Length == 0)
        {
            bodyText.text = string.Empty;
            return;
        }

        index = Mathf.Clamp(index, 0, lines.Length - 1);
        bodyText.text = lines[index];
    }

    private void Next()
    {
        if (lines == null || lines.Length == 0) return;

        index++;

        if (index >= lines.Length)
        {
            sequenceEnded = true;
            if (hintText != null) hintText.text = string.Empty;

            // ✅ 여기서 “대화 끝” → Retry 버튼 활성화
            SetRetryVisible(true);

            onSequenceFinished?.Invoke();
            return;
        }

        RefreshBody();
    }

    public void Hide()
    {
        showing = false;
        isTutorial = false;
        fading = false;
        sequenceEnded = false;

        if (pauseTimeScale)
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

        if (tutorialPanelRoot != null)
            tutorialPanelRoot.SetActive(false);

        SetRetryVisible(false);
        gameObject.SetActive(false);
    }
}
