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
    [SerializeField] private GameObject retryRoot;   // 버튼/패널 묶음
    [SerializeField] private Button retryButton;

    [Header("옵션")]
    [SerializeField] private bool pauseTimeAfterFade = true;
    [SerializeField] private bool showCursorOnGameOver = true;

    private bool showing;
    private bool waitingAnyKey;
    private bool fading;

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

        // "아무키"
        if (IsAnyKeyPressedThisFrame())
        {
            waitingAnyKey = false;
            SetRetryVisible(true);
        }
    }

    public void ShowDeath(string title = null)
    {
        if (rootGroup == null)
        {
            Debug.LogWarning("[GameOverUI] rootGroup(CanvasGroup)가 비어있음");
            return;
        }

        showing = true;
        fading = true;
        waitingAnyKey = false;

        gameObject.SetActive(true);

        // UI 초기 상태
        rootGroup.alpha = 0f;
        rootGroup.blocksRaycasts = true;
        rootGroup.interactable = true;

        if (titleText != null)
        {
            titleText.text = string.IsNullOrEmpty(title) ? defaultTitle : title;
            titleText.gameObject.SetActive(false); // 페이드 끝난 뒤 "확" 등장
        }

        SetRetryVisible(false);

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
        // 타임스케일 영향 없이 페이드
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

        // 여기서 시간 멈춤
        if (pauseTimeAfterFade)
            Time.timeScale = 0f;

        fading = false;
        waitingAnyKey = true; // 이제 아무키 입력을 기다림
    }

    private bool IsAnyKeyPressedThisFrame()
    {
        // 키보드
        if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame)
            return true;

        // 마우스
        if (Mouse.current != null)
        {
            if (Mouse.current.leftButton.wasPressedThisFrame) return true;
            if (Mouse.current.rightButton.wasPressedThisFrame) return true;
            if (Mouse.current.middleButton.wasPressedThisFrame) return true;
        }

        // 게임패드(있으면)
        if (Gamepad.current != null)
        {
            var g = Gamepad.current;
            if (g.buttonSouth.wasPressedThisFrame) return true;
            if (g.buttonNorth.wasPressedThisFrame) return true;
            if (g.buttonWest.wasPressedThisFrame) return true;
            if (g.buttonEast.wasPressedThisFrame) return true;
            if (g.startButton.wasPressedThisFrame) return true;
            if (g.selectButton.wasPressedThisFrame) return true;
            if (g.leftShoulder.wasPressedThisFrame) return true;
            if (g.rightShoulder.wasPressedThisFrame) return true;
        }

        return false;
    }

    private void SetRetryVisible(bool visible)
    {
        if (retryRoot != null) retryRoot.SetActive(visible);
        else if (retryButton != null) retryButton.gameObject.SetActive(visible);
    }

    public void Hide()
    {
        showing = false;
        waitingAnyKey = false;
        fading = false;

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

        SetRetryVisible(false);

        // UI 오브젝트는 꺼도 되는데, Instance 때문에 씬에 항상 켜두는 쪽을 추천
        // 필요하면 아래 줄 주석 처리/해제 선택
        // gameObject.SetActive(false);
    }
}
