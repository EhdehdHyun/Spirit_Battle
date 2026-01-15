using UnityEngine;

public class ObjectTrigger : MonoBehaviour, IInteractable
{
    [Header("연결")]
    public MapIconUI linkedMapIcon;

    private bool isActivated = false;

    public void Interact(PlayerInteraction playerInteraction)
    {
        if (!isActivated)
        {
            ActivateBonfire();
        }
        else
        {
            Debug.Log("이미 활성화된 축복입니다. (휴식)");
        }

        PlayerCharacter player = playerInteraction.GetComponent<PlayerCharacter>();

        if (player != null)
        {
            player.Rest();
        }
    }

    private void ActivateBonfire()
    {
        isActivated = true;

        if (linkedMapIcon != null)
        {
            linkedMapIcon.UnlockIcon();
        }

        Debug.Log("축복 발견! 지도 아이콘 해금됨.");
    }

    public string GetInteractPrompt()
    {
        return "[F] 축복에서 휴식";
    }
}