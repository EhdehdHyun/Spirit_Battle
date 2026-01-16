using System.Collections;
using UnityEngine;
using TMPro; // UI 관련
using System.IO; // 파일 삭제를 위해 필수
using UnityEngine.SceneManagement; // 씬 재시작을 위해 필수

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
        // 1. ESC 메뉴 토글
        if (!IsUIBlocked && Input.GetKeyDown(KeyCode.Escape))
        {
            ToggleMenu();
        }

        // ▼▼▼ [추가된 기능] F1 키로 데이터 초기화 및 재시작 ▼▼▼
        if (Input.GetKeyDown(KeyCode.F1))
        {
            ResetDataAndRestart();
        }
    }

    // 초기화 및 재시작 함수
    private void ResetDataAndRestart()
    {
        Debug.Log("[GameManager] F1 눌림: 데이터 초기화 진행");

        // 1. 저장 파일 경로
        string path = Path.Combine(Application.persistentDataPath, "savefile.json");

        // 2. 파일 삭제
        if (File.Exists(path))
        {
            File.Delete(path);
            Debug.Log("세이브 파일이 삭제되었습니다.");
        }

        // 3. 메모리 상의 데이터 초기화
        if (Data != null)
        {
            Data.CurrentData = new SaveData();
        }

        // 4. 시간 스케일 복구 (일시정지 상태일 수 있으므로)
        Time.timeScale = 1f;

        // 5. 현재 씬 재시작
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
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

    // 튜토리얼 완료 저장
    public void CompleteTutorial()
    {
        if (Data.CurrentData != null)
        {
            // 현재 위치와 스탯도 같이 저장 (중요)
            if (playerStat != null)
            {
                playerStat.SaveToData(Data.CurrentData);
            }

            Data.CurrentData.isTutorialClear = true;
            Data.Save();
            Debug.Log("[GameManager] 튜토리얼 완료 저장됨.");
        }
    }
}