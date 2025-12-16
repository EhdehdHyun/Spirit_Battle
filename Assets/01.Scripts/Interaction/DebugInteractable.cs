using UnityEngine;

public class DebugInteractable : MonoBehaviour, IInteractable
{
    [TextArea]
    public string promptText = "F : 상호작용 테스트";

    // F를 눌렀을 때 호출됨
    public void Interact(PlayerInteraction player)
    {
        Debug.Log("DebugInteractable 상호작용 실행됨!");
    }

    // 플레이어 머리 위에 띄울 안내 문구
    public string GetInteractPrompt()
    {
        return promptText;
    }
}
