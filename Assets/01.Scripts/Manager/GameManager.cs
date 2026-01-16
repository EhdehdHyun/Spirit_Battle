using System.Collections;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public DataManager Data { get; private set; }

    [Header("Manager References")]
    public PlayerStat playerStat;

    public InventoryManager inventoryManager;

    [Header("UI Objects")]
    public GameObject menuPanel;
    public GameObject saveMessageText;

    public bool IsUIBlocked { get; private set; }
    private bool isMenuOpen = false;

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        Data = new DataManager();
        Data.Initialize();

        if (menuPanel != null) menuPanel.SetActive(false);
        if (saveMessageText != null) saveMessageText.SetActive(false);
    }

    void Start()
    {
        StartCoroutine(LoadGameCo());
    }

    IEnumerator LoadGameCo()
    {
        yield return null;
        yield return new WaitForEndOfFrame();

        if (Data.CurrentData != null)
        {
            Debug.Log("[GameManager] 저장된 데이터 로드 시작...");
            if (playerStat != null)
            {
                playerStat.LoadFromData(Data.CurrentData);
            }

            // 인벤토리 아이템 로드
            if (inventoryManager != null)
            {
                inventoryManager.LoadFromData(Data.CurrentData);
            }
        }
    }

    void Update()
    {
        if (!IsUIBlocked && Input.GetKeyDown(KeyCode.Escape))
        {
            ToggleMenu();
        }
    }

    public void ToggleMenu()
    {
        isMenuOpen = !isMenuOpen;

        if (menuPanel != null)
            menuPanel.SetActive(isMenuOpen);

        if (isMenuOpen)
        {
            Time.timeScale = 0f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Time.timeScale = 1f;

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    public void OnSaveButtonClick()
    {
        if (playerStat != null)
        {
            playerStat.SaveToData(Data.CurrentData);
        }

        if (inventoryManager != null)
        {
            inventoryManager.SaveToData(Data.CurrentData);
        }

        Data.Save();

        StartCoroutine(ShowSaveMessageRoutine());
    }

    public void OnExitButtonClick()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    IEnumerator ShowSaveMessageRoutine()
    {
        if (saveMessageText != null)
        {
            saveMessageText.SetActive(true);
            yield return new WaitForSecondsRealtime(2f);
            saveMessageText.SetActive(false);
        }
    }

    public void SetUIBlock(bool blocked)
    {
        IsUIBlocked = blocked;
    }
    public void CompleteTutorial()
    {
        if (Data.CurrentData != null)
        {
            if (playerStat != null)
            {
                playerStat.SaveToData(Data.CurrentData);
            }
            Data.CurrentData.isTutorialClear = true;
            Data.Save();
            Debug.Log("[GameManager] 튜토리얼 완료! 현재 위치와 함께 저장되었습니다.");
        }
    }
}