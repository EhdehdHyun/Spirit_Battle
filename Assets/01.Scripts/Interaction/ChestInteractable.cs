using System;
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
    
    [Header("튜토리얼 연출")]
    [SerializeField] private WorldArrowController worldArrow;

    private bool isOpened;

    // 아이템 데이터 로더(한 번만 생성해서 공유)
    private static Data_tableLoader dataLoader;

    private void Awake()
    {
        // Animator 자동 할당(Inspector에서 비워놔도 됨)
        if (animator == null)
            animator = GetComponent<Animator>();

        // Data_tableLoader 준비
        if (dataLoader == null)
        {
            try
            {
                dataLoader = new Data_tableLoader();    // Resources/JSON/Data_table 로딩
                Debug.Log("[ChestInteractable] Data_tableLoader 생성 완료");
            }
            catch (Exception e)
            {
                Debug.LogError($"[ChestInteractable] Data_tableLoader 생성 실패: {e.Message}");
            }
        }
    }

    // 플레이어가 바라보고 있을 때 표시할 안내 문구
    public string GetInteractPrompt()
    {
        if (openOnlyOnce && isOpened)
            return string.Empty;   // 이미 열렸으면 문구 안 띄우기

        return "Press [F]";
    }

    // PlayerInteraction 에서 F 눌렀을 때 호출
    public void Interact(PlayerInteraction player)
    {
        Debug.Log("[ChestInteractable] Interact 호출");
        if (worldArrow != null && worldArrow.gameObject.activeSelf)
        {
            worldArrow.gameObject.SetActive(false);
        }
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

        // 데이터 테이블에서 코인 데이터 찾기
        var data = dataLoader.GetByKey(rewardItemKey);
        if (data == null)
        {
            Debug.LogWarning($"[ChestInteractable] rewardItemKey {rewardItemKey} 에 해당하는 데이터가 없습니다.");
            return;
        }

        // 2-1) 인벤토리에 자동 추가
        if (autoPickup && InventoryManager.Instance != null)
        {
            var item = new ItemInstance(data, rewardAmount);
            InventoryManager.Instance.AddItem(item);
            Debug.Log($"[ChestInteractable] 인벤토리에 보상 추가 : {data.ItemName} x{rewardAmount}");
        }
        else if (InventoryManager.Instance == null)
        {
            Debug.LogWarning("[ChestInteractable] InventoryManager.Instance 가 없습니다.");
        }

        // 2-2) 씬에 코인 프리팹 스폰 (연출용)
        if (coinPrefab != null)
        {
            Transform baseTransform = spawnPoint != null ? spawnPoint : transform;

            // 상자 위 살짝 떠 있게
            Vector3 pos = baseTransform.position + Vector3.up * 0.5f;
            Quaternion rot = baseTransform.rotation;

            var coinObj = Instantiate(coinPrefab, pos, rot);
            Debug.Log($"[ChestInteractable] 코인 프리팹 스폰 : {coinObj.name}");

            // 필요하면 일정 시간 후 파괴
            if (coinLifetime > 0f)
                Destroy(coinObj, coinLifetime);

            // 나중에 “코인도 F 눌러서 줍고 싶다”면 여기에 ItemPickupFromTable 붙여서 세팅해주면 됨
             var pickup = coinObj.GetComponent<ItemPickupFromTable>();
             if (pickup != null)
             {
                 pickup.itemKey = rewardItemKey;
                 pickup.quantity = rewardAmount;
            }
        }
    }
}
