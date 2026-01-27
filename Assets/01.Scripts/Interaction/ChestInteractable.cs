using System;
using System.Collections;
using System.Collections.Generic; // List 사용을 위해 추가
using UnityEngine;
using TMPro;

public class ChestInteractable : MonoBehaviour, IInteractable
{
    [Header("몬스터 설정 (특정 몬스터 처치 필수)")]
    [Tooltip("이 리스트에 몬스터를 넣으면, 해당 몬스터들이 모두 죽어야만 상자가 열립니다.")]
    [SerializeField] private List<GameObject> guardMonsters;

    [Header("일반 몬스터 감지 (가디언 없을 때)")]
    [SerializeField] private float detectRadius = 5.0f;
    [SerializeField] private LayerMask monsterLayer;

    [Header("애니메이션 설정")]
    [SerializeField] private Animator animator;
    [SerializeField] private string openTriggerName = "Open";
    [SerializeField] private bool openOnlyOnce = true;

    [Header("보상 아이템 설정")]
    [SerializeField] private int rewardItemKey = 2002;
    [SerializeField] private int rewardAmount = 1;

    [Header("코인 스폰 설정")]
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private GameObject coinPrefab;
    [SerializeField] private bool autoPickup = true;
    [SerializeField] private float coinLifetime = 2f;

    [Header("상자 제거 설정")]
    [SerializeField] private float chestVanishDelay = 2f;
    [SerializeField] private bool disableInsteadOfDestroy = false;

    [Header("튜토리얼 연출")]
    [SerializeField] private WorldArrowController worldArrow;
    
    [Header("튜토리얼 전용")]
    [SerializeField] private bool isTutorialChest = false;

    private bool isOpened;
    private bool vanishScheduled;
    private Coroutine warningCoroutine;
    private static Data_tableLoader dataLoader;

    private void Awake()
    {
        if (animator == null) animator = GetComponent<Animator>();

        if (dataLoader == null)
        {
            try { dataLoader = new Data_tableLoader(); }
            catch (Exception e) { Debug.LogError($"Loader Error: {e.Message}"); }
        }

    }

    public string GetInteractPrompt()
    {
        if (openOnlyOnce && isOpened) return string.Empty;
        return "Press [F]";
    }

    public void Interact(PlayerInteraction player)
    {
        if (openOnlyOnce && isOpened) return;

        if (AreGuardiansAlive())
        {
            ShowWarningMessage("상자를 지키는 몬스터를 먼저 처치해야 합니다!");
            return;
        }

        if (guardMonsters.Count == 0 && CheckMonsterNearby())
        {
            ShowWarningMessage("주변에 몬스터가 있어 상자를 열 수 없습니다.");
            return;
        }

        isOpened = true;
        if (animator != null) animator.SetTrigger(openTriggerName);
        GiveReward(player);

        if (isTutorialChest && TutorialManager.Instance != null)
        {
            TutorialManager.Instance.ShowMoveForwardText();
        }
    }

    private bool AreGuardiansAlive()
    {
        if (guardMonsters == null || guardMonsters.Count == 0) return false;

        foreach (GameObject monster in guardMonsters)
        {
            if (monster != null && monster.activeInHierarchy)
            {
                return true;
            }
        }
        return false;
    }

    private bool CheckMonsterNearby()
    {
        return Physics.CheckSphere(transform.position, detectRadius, monsterLayer);
    }
    private void ShowWarningMessage(string message)
    {
        TutorialManager.Instance?.ShowSimpleMessage(message, 2.0f);
    }

    private void OnDrawGizmosSelected()
    {
        if (guardMonsters != null && guardMonsters.Count > 0)
        {
            Gizmos.color = Color.yellow;
            foreach (var monster in guardMonsters)
            {
                if (monster != null)
                    Gizmos.DrawLine(transform.position, monster.transform.position);
            }
        }
        else
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, detectRadius);
        }
    }

    private void GiveReward(PlayerInteraction player)
    {
        if (dataLoader == null) return;
        var data = dataLoader.GetByKey(rewardItemKey);
        if (data == null) return;

        if (autoPickup && InventoryManager.Instance != null)
        {
            var item = new ItemInstance(data, rewardAmount);
            InventoryManager.Instance.AddItem(item);
            ScheduleVanish();
        }

        if (coinPrefab != null)
        {
            Transform baseTransform = spawnPoint != null ? spawnPoint : transform;
            var coinObj = Instantiate(coinPrefab, baseTransform.position + Vector3.up * 0.5f, baseTransform.rotation);
            if (coinLifetime > 0f) Destroy(coinObj, coinLifetime);

            var pickup = coinObj.GetComponent<ItemPickupFromTable>();
            if (pickup != null)
            {
                pickup.itemKey = rewardItemKey;
                pickup.quantity = rewardAmount;
                if (!autoPickup) { pickup.onPickedUp -= HandleCoinPickedUp; pickup.onPickedUp += HandleCoinPickedUp; }
            }
        }
    }

    private void HandleCoinPickedUp() { ScheduleVanish(); }
    private void ScheduleVanish() { if (!vanishScheduled) { vanishScheduled = true; StartCoroutine(CoVanish()); } }
    private IEnumerator CoVanish() { yield return new WaitForSeconds(chestVanishDelay); if (disableInsteadOfDestroy) gameObject.SetActive(false); else Destroy(gameObject); }
}