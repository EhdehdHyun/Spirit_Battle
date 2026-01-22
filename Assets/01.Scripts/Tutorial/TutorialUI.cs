using TMPro;
using UnityEngine;
using System.Collections;

public class TutorialUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI guideText;

    private Coroutine hideCoroutine;

    public void Show(string message, float duration = 0f)
    {
        guideText.text = message;
        gameObject.SetActive(true);

        // 기존 타이머 중복 방지
        if (hideCoroutine != null)
            StopCoroutine(hideCoroutine);

        if (duration > 0f)
            hideCoroutine = StartCoroutine(HideAfterDelay(duration));
    }

    public void Hide()
    {
        if (hideCoroutine != null)
        {
            StopCoroutine(hideCoroutine);
            hideCoroutine = null;
        }

        gameObject.SetActive(false);
    }

    private IEnumerator HideAfterDelay(float time)
    {
        yield return new WaitForSecondsRealtime(time);
        Hide();
    }
}