using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class EyeBlinkController : MonoBehaviour
{
    public Image eyeImage;

    void Start()
    {
        StartCoroutine(BlinkRoutine());
    }

    IEnumerator BlinkRoutine()
    {
        // 완전 감김
        SetAlpha(1f);
        yield return new WaitForSeconds(1.5f);

        // 깜빡 2~3회
        yield return Blink();
        yield return Blink();

        // 완전히 눈 뜸
        yield return Fade(1f, 0f, 2f);
        gameObject.SetActive(false);
    }

    IEnumerator Blink()
    {
        yield return Fade(1f, 0.3f, 1f);
        yield return new WaitForSeconds(0.3f);
        yield return Fade(0.3f, 1f, 2f);
        yield return new WaitForSeconds(1f);
    }

    IEnumerator Fade(float from, float to, float time)
    {
        float t = 0;
        while (t < time)
        {
            t += Time.deltaTime;
            SetAlpha(Mathf.Lerp(from, to, t / time));
            yield return null;
        }
    }
    
    

    void SetAlpha(float a)
    {
        var c = eyeImage.color;
        eyeImage.color = new Color(c.r, c.g, c.b, a);
    }
}