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

    [Header("UI Objects")]
    public GameObject menuPanel;
    public GameObject saveMessageText;

    [Header("Other UI Panels (�ߺ� ������ ���� �ʼ�)")]
    public GameObject inventoryPanel;
    public GameObject mapPanel;
    public GameObject questPanel;

    [Header("Menu Buttons (Inspector ���� �ʼ�)")]
    public Button saveButton;
    public Button exitButton;

    [Header("���� ���� ���� (���� ����)")]
    public float saveCheckRadius = 15f;
    public LayerMask enemyLayer;

    


    public bool IsUIBlocked { get; private set; }
    private bool isMenuOpen = false;

    public bool IsAnyPopupOpen
    {
        get
        {
            bool isSaveMenuOpen = (menuPanel != null && menuPanel.activeSelf);
            bool isInvOpen = (inventoryPanel != null && inventoryPanel.activeSelf);
            bool isMapOpen = (mapPanel != null && mapPanel.activeSelf);
            bool isQuestOpen = (questPanel != null && questPanel.activeInHierarchy);

            return isSaveMenuOpen || isInvOpen || isMapOpen || isQuestOpen;
        }
    }
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(Instance.gameObject);
        }

        Instance = this;

        Data = new DataManager();
        Data.Initialize();

        // UI �ʱ�ȭ
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
            Debug.Log("[GameManager] ����� ������ �ε� ����...");
            if (playerStat != null)
            {
                playerStat.LoadFromData(Data.CurrentData);
            }

            // �κ��丮 ������ �ε�
            if (inventoryManager != null)
            {
                inventoryManager.LoadFromData(Data.CurrentData);
            }
        }
    }

    void Update()
    {
        if (!IsUIBlocked && !GlobalInputBlocker.IsKeyBlocked(KeyCode.P) && Input.GetKeyDown(KeyCode.P))
        {
            if (isMenuOpen || !IsAnyPopupOpen)
            {
                ToggleMenu();
            }
        }

        // 2. F1 Ű�� ������ �ʱ�ȭ �� �����
        if (Input.GetKeyDown(KeyCode.F1))
        {
            ResetDataAndRestart();
        }
    }
    private void ResetDataAndRestart()
    {
        Debug.Log("[GameManager] F1 ����: ������ �ʱ�ȭ ����");

        // 1. ���� ���� ���
        string path = Path.Combine(Application.persistentDataPath, "savefile.json");

        // 2. ���� ����
        if (File.Exists(path))
        {
            File.Delete(path);
            Debug.Log("���̺� ������ �����Ǿ����ϴ�.");
        }

        // 3. �޸� ���� ������ �ʱ�ȭ
        if (Data != null)
        {
            Data.CurrentData = new SaveData();
        }

        // 4. �ð� ������ ����
        Time.timeScale = 1f;

        // 5. ���� �� �����
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

    // ��ư Ȱ��ȭ ���� ����
    private void UpdateMenuButtons()
    {
        bool canSave = false;

        // 1. Ʃ�丮�� �Ϸ� ���� Ȯ��
        if (Data != null && Data.CurrentData != null)
        {
            canSave = Data.CurrentData.isTutorialClear;
        }

        // 2. �ֺ� ���� ����
        bool enemiesNearby = CheckEnemiesNearby();
        if (enemiesNearby)
        {
            canSave = false;
            Debug.Log("�ֺ��� ���Ͱ� �־� ������ �� �����ϴ�.");
        }

        // 3. ��ư ���� ����
        if (saveButton != null)
        {
            saveButton.interactable = canSave;

            // (�ɼ�) ��ư �ؽ�Ʈ ���� ���
            TextMeshProUGUI btnText = saveButton.GetComponentInChildren<TextMeshProUGUI>();
            if (btnText != null)
            {
                if (enemiesNearby) btnText.text = "���� ��";
                else if (!Data.CurrentData.isTutorialClear) btnText.text = "���� �ʿ�";
                else btnText.text = "�����ϱ�";
            }
        }
        if (exitButton != null) exitButton.interactable = canSave;
    }

    // �ֺ� ���� ���� ����
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