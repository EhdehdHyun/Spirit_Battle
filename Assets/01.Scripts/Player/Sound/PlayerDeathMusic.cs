using UnityEngine;
using System.Collections;

public class PlayerDeathMusic : MonoBehaviour
{
    private CharacterBase _character;

    [Header("설정")]
    [Tooltip("플레이어 사망 후 음악이 바뀔 때까지의 대기 시간 (즉시는 0)")]
    public float delayTime = 0f;

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
        if (delayTime > 0f)
            yield return new WaitForSeconds(delayTime);
        if (GameManager.Instance != null && GameManager.Instance.mainBgm != null)
        {
            SoundManager.Instance.PlayBGM(GameManager.Instance.mainBgm);
        }
    }
}