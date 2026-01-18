using UnityEngine;

public class InvestigateInteractable : MonoBehaviour, IInteractable
{
    [Header("Investigate")]
    [SerializeField] private int investigateID; // Quest TargetID (ex: 6000)

    private bool used = false;

    private void Awake()
    {
        QuestTargetRegistry.Instance.Register(investigateID, transform);
    }
    
    public string GetInteractPrompt()
    {
        if (used) return string.Empty;
        return "조사하기 [F]";
    }

    public void Interact(PlayerInteraction player)
    {
        if (used) return;
        used = true;

        Debug.Log($"[Investigate] 조사 완료 ID={investigateID}");

        QuestManager.Instance.OnInvestigate(investigateID);
        // 조사 완료 후 더 이상 목표 아님 H(UDUI)
        QuestTargetRegistry.Instance.Unregister(investigateID);

        Debug.Log($"[Investigate] 조사 완료 ID={investigateID}");
        // 선택: 조사 후 비활성화
        // gameObject.SetActive(false);
    }
}