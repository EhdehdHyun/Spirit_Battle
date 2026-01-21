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
        if (current == null)
        {
            TryQueueRespawn();
            return;
        }
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

        if (current != null)
            Destroy(current);

        SpawnNow();
        respawnQueued = false;
    }

    private void SpawnNow()
    {
        if (enemyPrefab == null)
        {
            return;
        }

        current = Instantiate(enemyPrefab, transform.position, transform.rotation);
    }
}
