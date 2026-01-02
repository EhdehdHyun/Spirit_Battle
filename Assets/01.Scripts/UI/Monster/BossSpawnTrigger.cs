using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossSpawnTrigger : MonoBehaviour
{
    [Header("보스 오브젝트들")]
    public GameObject bossRoot;
    public BossEnemy boss;

    private bool spawned = false;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (spawned) return;

        spawned = true;

        if (bossRoot != null)
            bossRoot.SetActive(true);

        if (BossUIStatus.Instance != null && boss != null)
        {
            BossUIStatus.Instance.SetBoss(boss);
        }
    }
}
