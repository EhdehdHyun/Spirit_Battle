using UnityEngine;

public class QuestUIController : MonoBehaviour
{
    [SerializeField] private GameObject questCanvasRoot;
    [SerializeField] private QuestCategoryUI mainCategoryUI;

    private bool isOpen = false;

    void Start()
    {
        questCanvasRoot.SetActive(false);
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
            mainCategoryUI.SelectFirstQuest();
        }

        Debug.Log("[QuestUIController] SetActive = " + isOpen);
    }

}