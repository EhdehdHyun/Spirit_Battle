using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.IO;
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
    public GameObject menuPanel;       // 설정(메뉴) 창 (P키)
    public GameObject saveMessageText;

    [Header("Other UI Panels")]
    public GameObject inventoryPanel;
    public GameObject mapPanel;        // 지도 (M키)
    public GameObject questPanel;

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

            return isSaveMenuOpen || isInvOpen || isMapOpen;
        }
    }

    void Awake()
    {
        if (Instance != null && Instance != this) Destroy(Instance.gameObject);
        Instance = this;

        Data = new DataManager();
        Data.Initialize();

        if (menuPanel != null) menuPanel.SetActive(false);
        if (mapPanel != null) mapPanel.SetActive(false);
        if (saveMessageText != null) saveMessageText.SetActive(false);
    }

    void Start()
    {
        StartCoroutine(LoadGameCo());
        if (SoundManager.Instance != null && mainBgm != null)
        {
            SoundManager.Instance.PlayBGM(mainBgm);
        }
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
        if (!IsUIBlocked && !GlobalInputBlocker.IsKeyBlocked(KeyCode.P) && Input.GetKeyDown(KeyCode.P))
        {
            if (isMenuOpen)
            {
                ToggleMenu();
            }
            else
            {
                CloseAllPopups();
                ToggleMenu();
            }
        }
        if (!IsUIBlocked && !GlobalInputBlocker.IsKeyBlocked(KeyCode.M) && Input.GetKeyDown(KeyCode.M))
        {
            if (!isMenuOpen)
            {
                if (isMapOpen)
                {
                    ToggleMap();
                }
                else
                {
                    if (inventoryPanel != null && inventoryPanel.activeSelf)
                    {
                        inventoryManager.ToggleInventory();
                    }
                    ToggleMap();
                }
            }
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
            questPanel.SetActive(false);
        }
    }

    public void ToggleMenu()
    {
        isMenuOpen = !isMenuOpen;
        if (menuPanel != null) menuPanel.SetActive(isMenuOpen);

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
    public void ToggleMap()
    {
        isMapOpen = !isMapOpen;

        if (mapPanel != null)
            mapPanel.SetActive(isMapOpen);

        if (isMapOpen)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
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
        //if (exitButton != null) exitButton.interactable = canSave; // 종료 버튼 인터렉터블 비활성화 코드
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