using System.Collections;
using UnityEngine;

public class EnemyRespawnPoint : MonoBehaviour
{
    [Header("스폰할 적 프리팹(일반/엘리트)")]
    [SerializeField] private GameObject enemyPrefab;

    [Header("부활 시간(초)")]
    [SerializeField] private float respawnDelay = 10f;

    [Header("옵션")]
    [SerializeField] private bool spawnOnStart = true;

    private GameObject current;
    private bool respawnQueued = false;

    private void Start()
    {
        if (spawnOnStart)
            SpawnNow();
    }

    private void Update()
    {
        // 1) Destroy 되어 null이면 리스폰 예약
        if (current == null)
        {
            TryQueueRespawn();
            return;
        }

        // 2) 안 죽고 남아있지만 HP=0 상태면(애니 이벤트 누락 같은 케이스)도 리스폰 예약
        var cb = current.GetComponent<CharacterBase>();
        if (cb != null && !cb.IsAlive)
        {
            TryQueueRespawn();
        }
    }

    private void TryQueueRespawn()
    {
        if (respawnQueued) return;
        respawnQueued = true;
        StartCoroutine(RespawnRoutine());
    }

    private IEnumerator RespawnRoutine()
    {
        yield return new WaitForSeconds(respawnDelay);

        // 혹시 시체가 남아있으면 치워주기
        if (current != null)
            Destroy(current);

        SpawnNow();
        respawnQueued = false;
    }

    private void SpawnNow()
    {
        if (enemyPrefab == null)
        {
            Debug.LogWarning("[EnemyRespawnPoint] enemyPrefab이 비어있음");
            return;
        }

        current = Instantiate(enemyPrefab, transform.position, transform.rotation);
    }
}
