using System.Collections;
using UnityEngine;

public class BossSpawnInteratableOnce : MonoBehaviour, IInteractable
{
    [Header("Prompt")]
    [SerializeField] private string prompt = "Press [F]";

    [Header("Boss Root (비활성화로 씬에 배치)")]
    [SerializeField] private GameObject bossRoot;
    [SerializeField] private BossEnemy boss;

    [Header("등장 연출 시간")]
    [SerializeField] private float spawnDelay = 0f;

    [Header("옵션")]
    [SerializeField] private bool linkBossUIOnSpawn = true;
    [SerializeField] private bool disableColliderOnUse = true;

    private bool used = false;
    private Coroutine co;
    private Collider col;

    private void Awake()
    {
        col = GetComponent<Collider>();
    }


    public string GetInteractPrompt()
    {
        if (used) return string.Empty;
        return prompt;
    }

    public void Interact(PlayerInteraction player)
    {
        throw new System.NotImplementedException();
    }
}
