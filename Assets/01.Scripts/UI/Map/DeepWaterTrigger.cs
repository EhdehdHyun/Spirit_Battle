using UnityEngine;

public class DeepWaterTrigger : MonoBehaviour
{
    [Header("익사 설정")]
    [Tooltip("데미지가 들어오는 간격 (초)")]
    public float tickInterval = 1.0f;

    [Tooltip("틱당 스태미나 감소량")]
    public float staminaCost = 15f;

    [Tooltip("스태미나가 없을 때 틱당 체력 감소량")]
    public float healthDamage = 20f;

    private float _timer = 0f;
    private PlayerStat _playerStat;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            _playerStat = other.GetComponent<PlayerStat>();
            if (_playerStat == null)
                _playerStat = other.GetComponentInParent<PlayerStat>();

            if (_playerStat != null)
            {
                _playerStat.SetStaminaRegenBlock(true);
            }
            _timer = 0f;
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (_playerStat == null) return;
        if (!other.CompareTag("Player")) return;

        _timer += Time.deltaTime;

        if (_timer >= tickInterval)
        {
            _timer = 0f;
            ProcessDrowning();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (_playerStat != null)
            {
                _playerStat.SetStaminaRegenBlock(false);
            }

            _playerStat = null;
            _timer = 0f;
        }
    }

    private void ProcessDrowning()
    {
        if (_playerStat == null) return;
        bool staminaConsumed = _playerStat.TryConsumeStamina(staminaCost);

        if (staminaConsumed)
        {
            Debug.Log("스태미나 감소");
        }
        else
        {
            _playerStat.ApplyDrowningDamage(healthDamage);
        }
    }
}