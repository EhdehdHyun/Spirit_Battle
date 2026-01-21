using UnityEngine;

public class BossTriggerMusic : MonoBehaviour
{
    [Header("보스전 음악")]
    public AudioClip bossBgm;

    [Header("설정")]
    public bool playOnce = true;
    private bool _hasPlayed = false;

    private void OnTriggerEnter(Collider other)
    {
        if (playOnce && _hasPlayed) return;

        if (other.CompareTag("Player"))
        {
            if (SoundManager.Instance != null && bossBgm != null)
            {
                SoundManager.Instance.PlayBGM(bossBgm);
                _hasPlayed = true;
            }
        }
    }
}