using System.Collections;
using UnityEngine;

public class IntroController : MonoBehaviour
{
    public AudioSource stormAudio;
    public ParticleSystem stormRain;

    void Start()
    {
        if (stormAudio != null)
            stormAudio.Play();
        StartCoroutine(StopStormAfterTime(20f));
    }

    IEnumerator StopStormAfterTime(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (stormRain != null)
            stormRain.Stop();     // 비 멈춤

        if (stormAudio != null)
            stormAudio.Stop();   // 소리 멈춤
    }

}