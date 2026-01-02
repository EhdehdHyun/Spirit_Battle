using UnityEngine;

public class BeachWaveArea : MonoBehaviour
{
    [SerializeField] private AudioSource waveSource;

    private void Awake()
    {
        if (waveSource == null)
            waveSource = GetComponent<AudioSource>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        if (!waveSource.isPlaying)
            waveSource.Play();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        waveSource.Stop();
    }
}