using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.IO;

public class TitleMenuController : MonoBehaviour
{
    [Header("Scene Names")]
    [SerializeField] private string introSceneName = "IntroScene";

    [SerializeField] private string gameSceneName = "GameScene";

    [Header("UI References")]
    [SerializeField] private Button continueButton;
    [SerializeField] private GameObject notImplementedPopup;

    private string SavePath => Path.Combine(Application.persistentDataPath, "savefile.json");

    private void Start()
    {
        bool hasSaveData = File.Exists(SavePath);
        if (continueButton != null)
        {
            continueButton.interactable = hasSaveData;
        }

        if (notImplementedPopup != null)
        {
            notImplementedPopup.SetActive(false);
        }
    }

    public void OnClickNewGame()
    {
        if (File.Exists(SavePath))
        {
            File.Delete(SavePath);
        }
        SceneManager.LoadScene(introSceneName);
    }

    public void OnClickContinue()
    {
        if (File.Exists(SavePath))
        {
            SceneManager.LoadScene(gameSceneName);
        }
    }

    public void OnClickOptions()
    {
        if (notImplementedPopup != null)
        {
            notImplementedPopup.SetActive(true);
        }
    }

    public void OnClickClosePopup()
    {
        if (notImplementedPopup != null)
        {
            notImplementedPopup.SetActive(false);
        }
    }

    public void OnClickQuit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}