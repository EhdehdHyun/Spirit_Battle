using UnityEngine;

public class ObjectTrigger : MonoBehaviour, IInteractable
{
    [Header("연결 설정")]
    [Tooltip("연결된 맵 아이콘 UI를 할당하세요.")]
    public MapIconUI linkedMapIcon;

    [Tooltip("포탈의 시각 효과를 제어하는 Portal_Controller를 할당하세요.")]
    [SerializeField] private Portal_Controller portalController;

    private bool isActivated = false;

    public void Interact(PlayerInteraction playerInteraction)
    {
        if (!isActivated)
        {
            ActivatePortal();
        }
        PlayerCharacter player = playerInteraction.GetComponent<PlayerCharacter>();
        if (player != null)
        {
            player.Rest();
            Debug.Log("플레이어가 포탈에서 휴식합니다.");
        }
    }
    private void ActivatePortal()
    {
        isActivated = true;
        if (linkedMapIcon != null)
        {
            linkedMapIcon.UnlockIcon();
        }
        if (portalController != null)
        {
            portalController.TogglePortal(true);
        }
    }

    public string GetInteractPrompt()
    {
        return isActivated ? "[F] 휴식" : "[F] 포탈 활성화 및 휴식";
    }
    private void Reset()
    {
        if (portalController == null)
            portalController = GetComponentInChildren<Portal_Controller>();
    }
}