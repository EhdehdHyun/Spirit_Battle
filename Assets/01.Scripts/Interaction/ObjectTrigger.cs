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
    }

    public string GetInteractPrompt()
    {
        return "[F] 휴식";
    }
}