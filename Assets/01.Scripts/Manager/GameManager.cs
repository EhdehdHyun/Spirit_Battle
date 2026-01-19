using System.Collections;
using UnityEngine;
using TMPro; // UI 텍스트 관련
using UnityEngine.UI; // [필수] 버튼 제어를 위해 추가
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

    [Header("Menu Buttons (Inspector 연결 필수)")]
    public Button saveButton;  // 저장 버튼
    public Button exitButton;  // 종료 버튼

    [Header("전투 감지 설정 (저장 금지)")]
    public float saveCheckRadius = 15f;
    public LayerMask enemyLayer;

    public bool IsUIBlocked { get; private set; }
    private bool isMenuOpen = false;

    void Awake()
    {
        // 좀비 매니저 방지 (씬 재시작 시 기존 매니저 파괴)
        if (Instance != null && Instance != this)
        {
            Destroy(Instance.gameObject);
        }

        Instance = this;

        Data = new DataManager();
        Data.Initialize();

        // UI 초기화
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
        if (!IsUIBlocked && Input.GetKeyDown(KeyCode.P))
        {
            ToggleMenu();
        }

        // 2. F1 키로 데이터 초기화 및 재시작
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

        // 4. 시간 스케일 복구
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
            UpdateMenuButtons();
        }
        else
        {
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    // 버튼 활성화 여부 결정
    private void UpdateMenuButtons()
    {
        bool canSave = false;

        // 1. 튜토리얼 완료 여부 확인
        if (Data != null && Data.CurrentData != null)
        {
            canSave = Data.CurrentData.isTutorialClear;
        }

        // 2. 주변 몬스터 감지
        bool enemiesNearby = CheckEnemiesNearby();
        if (enemiesNearby)
        {
            canSave = false;
            Debug.Log("주변에 몬스터가 있어 저장할 수 없습니다.");
        }

        // 3. 버튼 상태 적용
        if (saveButton != null)
        {
            saveButton.interactable = canSave;

            // (옵션) 버튼 텍스트 변경 기능
            TextMeshProUGUI btnText = saveButton.GetComponentInChildren<TextMeshProUGUI>();
            if (btnText != null)
            {
                if (enemiesNearby) btnText.text = "전투 중";
                else if (!Data.CurrentData.isTutorialClear) btnText.text = "진행 필요";
                else btnText.text = "저장하기";
            }
        }
        if (exitButton != null) exitButton.interactable = canSave;
    }

    // 주변 몬스터 감지 로직
    private bool CheckEnemiesNearby()
    {
        if (playerStat == null) return false;
        return Physics.CheckSphere(playerStat.transform.position, saveCheckRadius, enemyLayer);
    }
    void OnDrawGizmosSelected()
    {
        if (playerStat != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(playerStat.transform.position, saveCheckRadius);
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
            playerStat?.SaveToData(Data.CurrentData);
            Data.CurrentData.isTutorialClear = true;
            Data.Save();

            if (isMenuOpen) UpdateMenuButtons();
        }
    }
}