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

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        if (questCanvasRoot != null && questCanvasRoot.activeSelf)
        {

        }
    }

    public void SetQuestUI(bool active)
    {
        if (questCanvasRoot != null)
            questCanvasRoot.SetActive(active);

        if (active)
        {
            RefreshAll();

            if (playerInput != null) playerInput.Lock();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            Time.timeScale = 0f;
        }
        else
        {
            detailUI.Clear();
            detailUI.gameObject.SetActive(false);
            
            Time.timeScale = 1f;

            if (playerInput != null)
            {
                playerInput.Unlock();
                playerInput.ResetInputState();
            }
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    public void ShowQuestDetail(Quest_Data_Table quest)
    {
        detailUI.gameObject.SetActive(true);
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

        if (mainCategoryUI != null)
            mainCategoryUI.SelectFirstQuest();
    }
}