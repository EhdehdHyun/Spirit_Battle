using UnityEngine;

public class QuestUIController : MonoBehaviour
{
    [SerializeField] private GameObject questCanvasRoot;
    [SerializeField] private QuestCategoryUI mainCategoryUI;
    
    [SerializeField] private QuestCategoryUI[] categoryUIs;
    [SerializeField] private QuestDetailUI detailUI;
    
    [SerializeField] private PlayerInputController playerInput;
    
    private bool isOpen = false;

    void Start()
    {
        questCanvasRoot.SetActive(false);
    }
    public void RefreshAll()
    {
        foreach (var category in categoryUIs)
            category.Refresh();

        detailUI.Clear();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.I))
        {
            Debug.Log("[QuestUIController] 눌렀음");
            Toggle();
        }
        if (Input.GetKeyDown(KeyCode.Escape) && isOpen)
        {
            Toggle();
        }
    }

    public void Toggle()
    {
        isOpen = !isOpen;
        questCanvasRoot.SetActive(isOpen);

        if (isOpen)
        {
            // 플레이어 입력 잠금
            if (playerInput != null)
                playerInput.Lock();

            // 마우스 UI용
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            mainCategoryUI.SelectFirstQuest();
        }
        else
        {
            // 플레이어 입력 복구
            if (playerInput != null)
            {
                playerInput.Unlock();
                playerInput.ResetInputState();
            }

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        Debug.Log("[QuestUIController] SetActive = " + isOpen);
    }

}