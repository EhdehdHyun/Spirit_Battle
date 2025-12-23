using UnityEngine;

public class BoxInteract : MonoBehaviour
{
    public bool canInteract = false;
    [Header("Tutorial")]
    [SerializeField] private GameObject guideText;

    private void Update()
    {
        if (!canInteract) return;

        if (Input.GetKeyDown(KeyCode.F))
        {
            Interact();
        }
    }

    private void Interact()
    {
        Debug.Log("상자 상호작용!");
        // 상자 열기 / 아이템 지급 등
        if (guideText != null)
            guideText.SetActive(false);
    }
}