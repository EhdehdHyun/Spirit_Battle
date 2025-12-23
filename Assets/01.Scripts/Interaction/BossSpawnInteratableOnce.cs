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
        //이미 사용했으면 프롬프트 안 뜨게
        if (used) return string.Empty;
        return prompt;
    }

    public void Interact(PlayerInteraction player)
    {
        if (used) return;
        if (co != null) return;

        used = true;

        // 다시는 레이캐스트에 안 걸리게 즉 다시 누를려고 할 때 상호작용 불가
        if (disableColliderOnUse && col != null)
            col.enabled = false;

        co = StartCoroutine(SpawnRoutine());
    }

    private IEnumerator SpawnRoutine()
    {
        if (spawnDelay > 0f)
            yield return new WaitForSeconds(spawnDelay);

        if (bossRoot != null)
            bossRoot.SetActive(true);

        // boss 참조 없으면 bossRoot에서 찾아오기
        if (boss == null && bossRoot != null)
            boss = bossRoot.GetComponentInChildren<BossEnemy>(true);

        // 보스 UI 연결(선택)
        if (linkBossUIOnSpawn && boss != null && BossUIStatus.Instance != null)
            BossUIStatus.Instance.SetBoss(boss);

        co = null;
    }
}
