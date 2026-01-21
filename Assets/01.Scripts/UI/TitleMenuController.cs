using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI; // 버튼 제어를 위해 필요
using System.IO;      // 파일 체크/삭제를 위해 필요

public class TitleMenuController : MonoBehaviour
{
    [Header("Scene Names")]
    [Tooltip("새로 시작 시 이동할 인트로 씬 이름")]
    [SerializeField] private string introSceneName = "IntroScene";

    [Tooltip("이어하기 시 이동할 실제 게임 씬 이름")]
    [SerializeField] private string gameSceneName = "GameScene";

    [Header("UI References")]
    [Tooltip("이어하기 버튼 (세이브 파일 없으면 비활성화용)")]
    [SerializeField] private Button continueButton;

    private string SavePath => Path.Combine(Application.persistentDataPath, "savefile.json");

    private void Start()
    {
        bool hasSaveData = File.Exists(SavePath);
        if (continueButton != null)
        {
            continueButton.interactable = hasSaveData;
        }
    }

    public void OnClickNewGame()
    {
        Debug.Log("New Game Clicked: 기존 데이터 삭제 및 인트로 시작");
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
            Debug.Log("Continue Clicked: 게임 씬으로 이동하여 로드 진행");
            SceneManager.LoadScene(gameSceneName);
        }
        else
        {
            Debug.Log("세이브 파일이 없습니다.");
        }
    }

    // 옵션 (미구현)
    public void OnClickOptions()
    {
        Debug.Log("Options Clicked (Not Implemented)");
    }

    // 게임 종료
    public void OnClickQuit()
    {
        Debug.Log("Quit Game");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}