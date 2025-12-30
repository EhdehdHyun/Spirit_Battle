using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class TitleSceneController : MonoBehaviour
{
    [Header("Loading Canvas")]
    [SerializeField] private Image loadingOverlay;     // LoadingOverlay (검은 Image)
    [SerializeField] private GameObject loadingSpinner; // LoadingSpinner (아이콘)

    [Header("Fade Setting")]
    [SerializeField] private float fadeTime = 1.2f;

    [Header("Title Motion (선택)")]
    [SerializeField] private RectTransform titleImage;
    [SerializeField] private float moveAmplitude = 3f;
    [SerializeField] private float moveSpeed = 0.6f;

    private Vector3 titleStartPos;
    private bool isFading;

    void Start()
    {
        // 타이틀 시작 위치 저장
        if (titleImage != null)
            titleStartPos = titleImage.localPosition;

        // 스피너는 처음엔 꺼둠
        if (loadingSpinner != null)
            loadingSpinner.SetActive(false);

        // 검은 화면에서 시작 → 페이드 인
        if (loadingOverlay != null)
            StartCoroutine(FadeIn());
    }

    void Update()
    {
        // 타이틀 이미지 흔들림
        if (titleImage != null)
        {
            float xOffset = Mathf.Sin(Time.time * moveSpeed) * moveAmplitude;
            titleImage.localPosition = titleStartPos + new Vector3(xOffset, 0, 0);
        }

        // 아무 키나 누르면 페이드 아웃만 실행
        if (isFading) return;

        if (Input.anyKeyDown)
        {
            isFading = true;
            StartCoroutine(FadeOutOnly());
        }
    }

    IEnumerator FadeIn()
    {
        float t = 1f;
        while (t > 0)
        {
            t -= Time.deltaTime / fadeTime;
            loadingOverlay.color = new Color(0, 0, 0, t);
            yield return null;
        }
    }

    IEnumerator FadeOutOnly()
    {
        // 스피너 ON
        if (loadingSpinner != null)
            loadingSpinner.SetActive(true);

        float t = 0f;
        while (t < 1)
        {
            t += Time.deltaTime / fadeTime;
            loadingOverlay.color = new Color(0, 0, 0, t);
            yield return null;
        }

        // ===== 나중에 여기서 씬 이동 =====
        // SceneManager.LoadScene("GameScene");
    }
}
