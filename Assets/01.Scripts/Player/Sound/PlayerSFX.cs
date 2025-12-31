using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class PlayerSFX : MonoBehaviour
{

    [Header("AUdio")]
    [SerializeField] private AudioSource sfxSource;

    [Header("Attack Hits")]
    [SerializeField] private AudioClip[] attackHitClips = new AudioClip[3];

    [Header("Parry")]
    [SerializeField] private AudioClip parrySuccessClip;

    [SerializeField] private float volume = 1f;

    public void PlayAttackHit(int comboIndex1to3)
    {
        if (!sfxSource) return;
        int idx = Mathf.Clamp(comboIndex1to3 - 1, 0, 2);
        var clip = (attackHitClips != null && attackHitClips.Length > idx) ? attackHitClips[idx] : null;
        if (clip) sfxSource.PlayOneShot(clip, volume);
    }

    public void PlayParrySuccess()
    {
        if (!sfxSource || !parrySuccessClip) return;
        sfxSource.PlayOneShot(parrySuccessClip, volume);
    }
}
