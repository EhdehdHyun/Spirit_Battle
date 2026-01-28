using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public DataManager Data { get; private set; }

    [Header("Manager References")]
    public PlayerStat playerStat;
    public InventoryManager inventoryManager;

    [Header("Audio")]
    public AudioClip mainBgm;

    [Header("UI Objects")]
    public GameObject menuPanel;
    public GameObject saveMessageText;

    [Header("Other UI Panels")]
    public GameObject inventoryPanel;
    public GameObject mapPanel;
    public GameObject questPanel;

    [Header("Keys")]
    public KeyCode questKey = KeyCode.I;

    [Header("Menu Buttons")]
    public Button saveButton;
    public Button exitButton;

    [Header("전투 감지 설정")]
    public float saveCheckRadius = 15f;
    public LayerMask enemyLayer;

    public bool IsUIBlocked { get; private set; }

    private bool isMenuOpen = false;
    private bool isMapOpen = false;

    public bool IsAnyPopupOpen
    {
        get
        {
            bool isSaveMenuOpen = (menuPanel != null && menuPanel.activeSelf);
            bool isInvOpen = (inventoryPanel != null && inventoryPanel.activeSelf);
            bool isMapOpen = (mapPanel != null && mapPanel.activeSelf);

            bool isQuestOpen = false;
            if (questPanel != null) isQuestOpen = questPanel.activeSelf;

            return isSaveMenuOpen || isInvOpen || isMapOpen || isQuestOpen;
        }
    }

    void Awake()
    {
        if (Instance != null && Instance != this) Destroy(Instance.gameObject);
        Instance = this;

        Data = new DataManager();
        Data.Initialize();
    }

    void Start()
    {
        if (menuPanel != null) menuPanel.SetActive(false);
        if (mapPanel != null) mapPanel.SetActive(false);
        if (saveMessageText != null) saveMessageText.SetActive(false);
        if (questPanel != null) questPanel.SetActive(false);

        StartCoroutine(LoadGameCo());
        if (SoundManager.Instance != null && mainBgm != null)
        {
            SoundManager.Instance.PlayBGM(mainBgm);
        }

        if (CursorManager.Instance != null)
            CursorManager.Instance.SetUIActive(false);
    }

    IEnumerator LoadGameCo()
    {
        yield return null;
        yield return new WaitForEndOfFrame();

        if (Data.CurrentData != null)
        {
            if (playerStat != null) playerStat.LoadFromData(Data.CurrentData);
            if (inventoryManager != null) inventoryManager.LoadFromData(Data.CurrentData);
        }
    }

    void Update()
    {
        if (IsUIBlocked) return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            HandleEscInput();
        }
        else if (!GlobalInputBlocker.IsKeyBlocked(KeyCode.M) && Input.GetKeyDown(KeyCode.M))
        {
            OpenMapAndCloseOthers();
        }
        else if (!GlobalInputBlocker.IsKeyBlocked(KeyCode.Tab) && Input.GetKeyDown(KeyCode.Tab))
        {
            OpenInventoryAndCloseOthers();
        }
        else if (!GlobalInputBlocker.IsKeyBlocked(questKey) && Input.GetKeyDown(questKey))
        {
            OpenQuestAndCloseOthers();
        }
    }

    private void OpenInventoryAndCloseOthers()
    {
        if (isMenuOpen) ToggleMenu();
        if (isMapOpen) ToggleMap();
        if (questPanel != null && questPanel.activeSelf) ToggleQuest();

        if (inventoryManager != null)
        {
            inventoryManager.ToggleInventory();
        }
    }

    private void OpenMapAndCloseOthers()
    {
        if (isMenuOpen) ToggleMenu();
        if (inventoryManager != null && inventoryPanel.activeSelf) inventoryManager.ToggleInventory();
        if (questPanel != null && questPanel.activeSelf) ToggleQuest();

        ToggleMap();
    }

    private void OpenQuestAndCloseOthers()
    {
        if (isMenuOpen) ToggleMenu();
        if (inventoryManager != null && inventoryPanel.activeSelf) inventoryManager.ToggleInventory();
        if (isMapOpen) ToggleMap();

        ToggleQuest();
    }

    private void HandleEscInput()
    {
        if (IsAnyPopupOpen)
        {
            CloseAllPopups();
        }
        else
        {
            ToggleMenu();
        }
    }

    public void CloseAllPopups()
    {
        if (inventoryManager != null && inventoryPanel != null && inventoryPanel.activeSelf)
        {
            inventoryManager.ToggleInventory();
        }
        if (isMapOpen)
        {
            ToggleMap();
        }
        if (questPanel != null && questPanel.activeSelf)
        {
            ToggleQuest();
        }
        if (isMenuOpen)
        {
            ToggleMenu();
        }
    }

    public void ToggleMenu()
    {
        isMenuOpen = !isMenuOpen;
        if (menuPanel != null) menuPanel.SetActive(isMenuOpen);

        if (CursorManager.Instance != null)
            CursorManager.Instance.SetUIActive(isMenuOpen);

        if (isMenuOpen)
        {
            Time.timeScale = 0f;
            UpdateMenuButtons();
        }
        else
        {
            Time.timeScale = 1f;
        }
    }

    public void ToggleMap()
    {
        isMapOpen = !isMapOpen;

        if (mapPanel != null)
            mapPanel.SetActive(isMapOpen);

        if (CursorManager.Instance != null)
            CursorManager.Instance.SetUIActive(isMapOpen);
    }

    public void ToggleQuest()
    {
        if (questPanel == null) return;

        bool isActive = !questPanel.activeSelf;

        if (QuestUIController.Instance != null)
        {
            QuestUIController.Instance.SetQuestUI(isActive);
        }
        else
        {
            questPanel.SetActive(isActive);
            if (isActive) Time.timeScale = 0f;
            else Time.timeScale = 1f;
        }

        if (CursorManager.Instance != null)
            CursorManager.Instance.SetUIActive(isActive);
    }

    private void UpdateMenuButtons()
    {
        bool canSave = false;
        if (Data != null && Data.CurrentData != null) canSave = Data.CurrentData.isTutorialClear;

        bool enemiesNearby = CheckEnemiesNearby();
        if (enemiesNearby) canSave = false;

        if (saveButton != null)
        {
            saveButton.interactable = canSave;
            TextMeshProUGUI btnText = saveButton.GetComponentInChildren<TextMeshProUGUI>();
            if (btnText != null)
            {
                if (enemiesNearby) btnText.text = "전투 중";
                else if (!Data.CurrentData.isTutorialClear) btnText.text = "진행 필요";
                else btnText.text = "저장하기";
            }
        }
    }

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
        if (playerStat != null) playerStat.SaveToData(Data.CurrentData);
        if (inventoryManager != null) inventoryManager.SaveToData(Data.CurrentData);
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

    public void SetUIBlock(bool blocked) => IsUIBlocked = blocked;

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