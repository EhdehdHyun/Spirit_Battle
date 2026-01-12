using UnityEngine;

public class ObjectTrigger : MonoBehaviour, IInteractable
{
    [Header("연결")]
    public MapIconUI linkedMapIcon; // 지도상의 UI 버튼 연결

    private bool isActivated = false;

    private void Start()
    {
    }
    public void Interact(PlayerInteraction playerInteraction)
    {
        if (!isActivated)
        {
            ActivateBonfire();
        }
        else
        {
            Debug.Log("이미 활성화된 축복입니다.");
        }
    }

    private void ActivateBonfire()
    {
        isActivated = true;

        if (linkedMapIcon != null)
        {
            linkedMapIcon.UnlockIcon();
        }

        Debug.Log("축복 발견!");
    }

    public string GetInteractPrompt()
    {
        return "[F] 상호작용";
    }
}