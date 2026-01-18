using UnityEngine;

public class DestroyQuestObject : MonoBehaviour, IInteractable
{
    [Header("Quest Target")]
    [SerializeField] private int destroyTargetID = 72;
    [SerializeField] private float destroyDelay = 0.3f;

    private bool isDestroyed = false;
    
    [SerializeField] private string interactPrompt = "오염된 핵을 파괴한다";

    private void Awake()
    {
        QuestTargetRegistry.Instance.Register(destroyTargetID, transform);
    }
    public void Interact(PlayerInteraction player)
    {
        if (isDestroyed) return;
        isDestroyed = true;

        // 퀘스트 진행 보고
        QuestManager.Instance.ReportProgress(
            CompleteCondition.DestroyObject,
            destroyTargetID,
            1
        );

        // 연출 (이펙트, 사운드)

        // 오브젝트 파괴
        Destroy(gameObject, destroyDelay);

        Debug.Log($"[DestroyObject] TargetID={destroyTargetID} 파괴됨");
        
    }
    public string GetInteractPrompt()
    {
        return interactPrompt;
    }
    private void OnDestroy()
    {
        if (QuestTargetRegistry.Instance != null)
            QuestTargetRegistry.Instance.Unregister(destroyTargetID, transform);
    }
}