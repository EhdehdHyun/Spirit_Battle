using UnityEngine;
using System.Collections;

public class BossDeathMusic : MonoBehaviour
{
    private CharacterBase _character;

    [Header("설정")]
    [Tooltip("보스가 죽고 나서 음악이 바뀔 때까지의 대기 시간")]
    public float delayTime = 3.0f;

    void Awake()
    {
        _character = GetComponent<CharacterBase>();
    }

    void OnEnable()
    {
        if (_character != null)
            _character.OnDied += HandleDie;
    }

    void OnDisable()
    {
        if (_character != null)
            _character.OnDied -= HandleDie;
    }

    private void HandleDie(DamageInfo info)
    {
        StartCoroutine(RestoreMusicRoutine());
    }

    IEnumerator RestoreMusicRoutine()
    {
        yield return new WaitForSeconds(delayTime);
        if (GameManager.Instance != null && GameManager.Instance.mainBgm != null)
        {
            SoundManager.Instance.PlayBGM(GameManager.Instance.mainBgm);
        }
    }
}