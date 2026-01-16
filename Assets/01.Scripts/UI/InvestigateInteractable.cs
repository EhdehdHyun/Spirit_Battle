using UnityEngine;

public class InvestigateInteractable : MonoBehaviour, IInteractable
{
    [Header("Investigate")]
    [SerializeField] private int investigateID; // Quest TargetID (ex: 6000)

    private bool used = false;

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

        // 선택: 조사 후 비활성화
        // gameObject.SetActive(false);
    }
}