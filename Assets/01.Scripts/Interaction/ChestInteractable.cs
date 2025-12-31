using System;
using System.Collections;
using UnityEngine;

public class ChestInteractable : MonoBehaviour, IInteractable
{
    [Header("애니메이션 설정")]
    [SerializeField] private Animator animator;        // TreasureChest에 붙은 Animator
    [SerializeField] private string openTriggerName = "Open";
    [SerializeField] private bool openOnlyOnce = true; // 한 번만 열리게

    [Header("보상 아이템 설정")]
    [SerializeField] private int rewardItemKey = 2002; // 코인 데이터 key (엑셀/JSON에 있는 key)
    [SerializeField] private int rewardAmount = 1;     // 몇 개 줄지

    [Header("코인 스폰 설정")]
    [SerializeField] private Transform spawnPoint;     // 코인이 나타날 위치(없으면 Chest 위치)
    [SerializeField] private GameObject coinPrefab;    // 코인 3D 프리팹
    [SerializeField] private bool autoPickup = true;   // 인벤토리에 자동으로 넣을지
    [SerializeField] private float coinLifetime = 2f;  // 연출용 코인 유지 시간(초). 0이면 안 지움

    [Header("상자 제거 설정")]
    [Tooltip("보상 획득(자동지급 or 코인줍기) 후 몇 초 뒤 상자를 제거할지")]
    [SerializeField] private float chestVanishDelay = 2f;
    [Tooltip("Destroy 대신 SetActive(false)로 숨길지")]
    [SerializeField] private bool disableInsteadOfDestroy = false;

    [Header("튜토리얼 연출")]
    [SerializeField] private WorldArrowController worldArrow;

    private bool isOpened;
    private bool vanishScheduled;

    // 아이템 데이터 로더(한 번만 생성해서 공유)
    private static Data_tableLoader dataLoader;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();

        if (dataLoader == null)
        {
            try
            {
                dataLoader = new Data_tableLoader();
                Debug.Log("[ChestInteractable] Data_tableLoader 생성 완료");
            }
            catch (Exception e)
            {
                Debug.LogError($"[ChestInteractable] Data_tableLoader 생성 실패: {e.Message}");
            }
        }
    }

    public string GetInteractPrompt()
    {
        if (openOnlyOnce && isOpened)
            return string.Empty;

        return "Press [F]";
    }

    public void Interact(PlayerInteraction player)
    {
        Debug.Log("[ChestInteractable] Interact 호출");

        if (openOnlyOnce && isOpened)
            return;

        isOpened = true;

        if (animator != null)
            animator.SetTrigger(openTriggerName);

        GiveReward(player);

        TutorialManager.Instance.ShowMoveForwardText();
    }

    private void GiveReward(PlayerInteraction player)
    {
        if (dataLoader == null)
        {
            Debug.LogError("[ChestInteractable] Data_tableLoader 가 없어 아이템 데이터를 가져올 수 없습니다.");
            return;
        }

        var data = dataLoader.GetByKey(rewardItemKey);
        if (data == null)
        {
            Debug.LogWarning($"[ChestInteractable] rewardItemKey {rewardItemKey} 에 해당하는 데이터가 없습니다.");
            return;
        }

        // ✅ 1) autoPickup이면 즉시 지급 → 즉시 상자 제거 예약
        if (autoPickup && InventoryManager.Instance != null)
        {
            var item = new ItemInstance(data, rewardAmount);
            InventoryManager.Instance.AddItem(item);
            Debug.Log($"[ChestInteractable] 인벤토리에 보상 추가 : {data.ItemName} x{rewardAmount}");

            ScheduleVanish(); // ✅ 자동지급이면 지금이 "획득 완료" 시점
        }
        else if (autoPickup && InventoryManager.Instance == null)
        {
            Debug.LogWarning("[ChestInteractable] InventoryManager.Instance 가 없습니다.");
        }

        // ✅ 2) 코인 프리팹 스폰 (연출/수동 줍기용)
        if (coinPrefab != null)
        {
            Transform baseTransform = spawnPoint != null ? spawnPoint : transform;

            Vector3 pos = baseTransform.position + Vector3.up * 0.5f;
            Quaternion rot = baseTransform.rotation;

            var coinObj = Instantiate(coinPrefab, pos, rot);
            Debug.Log($"[ChestInteractable] 코인 프리팹 스폰 : {coinObj.name}");

            // 연출용 수명
            if (coinLifetime > 0f)
                Destroy(coinObj, coinLifetime);

            // 코인에 pickup이 있으면 데이터 세팅
            var pickup = coinObj.GetComponent<ItemPickupFromTable>();
            if (pickup != null)
            {
                pickup.itemKey = rewardItemKey;
                pickup.quantity = rewardAmount;

                // ✅ autoPickup=false일 때만 "코인 줍기" 순간을 감지해서 상자 제거 예약
                if (!autoPickup)
                {
                    pickup.onPickedUp -= HandleCoinPickedUp; // 중복 방지
                    pickup.onPickedUp += HandleCoinPickedUp;
                }
            }
            else
            {
                if (!autoPickup)
                {
                    Debug.LogWarning("[ChestInteractable] autoPickup=false인데 coinPrefab에 ItemPickupFromTable이 없습니다. (줍기 감지 불가)");
                }
            }
        }
    }

    private void HandleCoinPickedUp()
    {
        // ✅ 코인을 실제로 주운 순간
        ScheduleVanish();
    }

    private void ScheduleVanish()
    {
        if (vanishScheduled) return;
        vanishScheduled = true;
        StartCoroutine(CoVanish());
    }

    private IEnumerator CoVanish()
    {
        yield return new WaitForSeconds(chestVanishDelay);

        if (disableInsteadOfDestroy)
            gameObject.SetActive(false);
        else
            Destroy(gameObject);
    }
}
