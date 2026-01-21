using UnityEngine;
using System.Collections;

public class QuestUIController : MonoBehaviour
{
    public static QuestUIController Instance;

    [SerializeField] private GameObject questCanvasRoot;
    [SerializeField] private QuestCategoryUI mainCategoryUI;

    [SerializeField] private QuestCategoryUI[] categoryUIs;
    [SerializeField] private QuestDetailUI detailUI;

    [SerializeField] private PlayerInputController playerInput;

    private bool isOpen = false;

    void Awake()
    {
        Debug.Log($"[QuestUIController] Toggle isOpen={isOpen}, root={questCanvasRoot.name}, rootId={questCanvasRoot.GetInstanceID()}");
        Instance = this;
    }

    void Start()
    {
        if (questCanvasRoot != null)
            questCanvasRoot.SetActive(false);
    }

    public void ShowQuestDetail(Quest_Data_Table quest)
    {
        detailUI.SetQuest(quest);
    }

    public void ClearDetail()
    {
        detailUI.Clear();
    }

    public void RefreshAll()
    {
        detailUI.Clear();
        foreach (var category in categoryUIs)
            category.Refresh();

        QuestItemUI.ResetSelection();
        StartCoroutine(SelectFirstQuestNextFrame());
    }

    private IEnumerator SelectFirstQuestNextFrame()
    {
        yield return null; // 다음 프레임
        mainCategoryUI.SelectFirstQuest();
    }

    void Update()
    {
        if (GlobalInputBlocker.IsKeyBlocked(KeyCode.I)) return;

        if (Input.GetKeyDown(KeyCode.I))
        {
            bool amIOpen = (questCanvasRoot != null && questCanvasRoot.activeSelf);

            if (amIOpen || !GameManager.Instance.IsAnyPopupOpen)
            {
                Debug.Log("[QuestUIController] 눌렀음");
                Toggle();
            }
        }
        if (Input.GetKeyDown(KeyCode.Escape) && isOpen)
        {
            Toggle();
        }
    }

    public void Toggle()
    {
        isOpen = !isOpen;

        if (questCanvasRoot != null)
            questCanvasRoot.SetActive(isOpen);

        if (isOpen)
        {
            if (playerInput != null)
                playerInput.Lock();

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            RefreshAll(); // 여기서만 처리
        }
        else
        {
            if (playerInput != null)
            {
                playerInput.Unlock();
                playerInput.ResetInputState();
            }

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}