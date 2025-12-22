using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EyeBlinkController : MonoBehaviour
{
    [Header("Eye Blink")]
    public Image eyeImage;

    [Header("Dialogue")]
    public TextMeshProUGUI dialogueText;

    [Header("Camera")]
    public Transform cameraTransform;
    public Quaternion leftRotation;
    public Quaternion rightRotation;

    void Start()
    {
        StartCoroutine(BlinkRoutine());
    }

    IEnumerator BlinkRoutine()
    {
        // 첫 눈 깜빡임 
        SetAlpha(1f);
        dialogueText.text = "으으.. 무슨일이 있던거지?";
        yield return new WaitForSeconds(3f);

        yield return Blink();
        yield return Fade(1f, 0f, 2f);

        //왼쪽 회전 + 눈 감기 
        yield return RotateCamera(cameraTransform.rotation, leftRotation, 1f);

        dialogueText.text = "...";
        yield return Fade(0f, 1f, 0.5f);
        yield return new WaitForSeconds(1f);

        //오른쪽 회전 + 눈 뜸 
        yield return RotateCamera(leftRotation, rightRotation, 1.5f);

        dialogueText.text = "배를 탔던 기억은 있었는데.. 저건 뭐지..?";
        yield return Fade(1f, 0f, 1.5f);

        yield return new WaitForSeconds(2f);
        dialogueText.text = "";

        gameObject.SetActive(false);
    }

    //눈 깜빡임 
    IEnumerator Blink()
    {
        yield return Fade(1f, 0.3f, 0.8f);
        yield return new WaitForSeconds(0.2f);
        yield return Fade(0.3f, 1f, 1f);
        yield return new WaitForSeconds(0.5f);
    }

    IEnumerator Fade(float from, float to, float time)
    {
        float t = 0f;
        while (t < time)
        {
            t += Time.deltaTime;
            SetAlpha(Mathf.Lerp(from, to, t / time));
            yield return null;
        }
    }

    void SetAlpha(float a)
    {
        Color c = eyeImage.color;
        eyeImage.color = new Color(c.r, c.g, c.b, a);
    }

    // 카메라 회전 
    IEnumerator RotateCamera(Quaternion from, Quaternion to, float time)
    {
        float t = 0f;
        while (t < time)
        {
            t += Time.deltaTime;
            cameraTransform.rotation = Quaternion.Slerp(from, to, t / time);
            yield return null;
        }
    }
}
