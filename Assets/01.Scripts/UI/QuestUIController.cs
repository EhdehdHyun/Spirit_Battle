using UnityEngine;

public class QuestUIController : MonoBehaviour
{
    [SerializeField] private GameObject questCanvasRoot;

    private bool isOpen = false;
    
    void Awake()
    {
        Debug.Log("[QuestUIController] Awake");
    }
    void OnEnable()
    {
        Debug.Log("[QuestUIController] OnEnable");
    }

    void Start()
    {
        Debug.Log("[QuestUIController] Start");
        questCanvasRoot.SetActive(false);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.I))
        {
            Debug.Log("[QuestUIController] I pressed");
            Toggle();
        }
    }

    public void Toggle()
    {
        isOpen = !isOpen;
        questCanvasRoot.SetActive(isOpen);
        Debug.Log("[QuestUIController] SetActive = " + isOpen);

        // 열릴 때 기본 선택 퀘스트 처리 (다음 단계용)
        //if (isOpen)
        //{
           // QuestUIEvents.OnQuestWindowOpened?.Invoke();
        //}
    }
}