using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class IntroController : MonoBehaviour
{
    public AudioSource stormAudio;
    [SerializeField] private float introDuration = 15f;
    [SerializeField] private string nextSceneName = "TutorialScene";

    void Start()
    {
        if (stormAudio != null)
            stormAudio.Play();

        StartCoroutine(IntroSequence());
    }

    private IEnumerator IntroSequence()
    {
        yield return new WaitForSeconds(introDuration);

        SceneManager.LoadScene(nextSceneName);
    }
}