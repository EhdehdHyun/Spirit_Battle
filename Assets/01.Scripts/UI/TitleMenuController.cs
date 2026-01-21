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
        // 게임 시작 시 세이브 파일이 있는지 확인
        bool hasSaveData = File.Exists(SavePath);

        // 세이브 파일이 없으면 이어하기 버튼 비활성화 (회색 처리)
        if (continueButton != null)
        {
            continueButton.interactable = hasSaveData;
        }
    }

    // 새로 시작
    public void OnClickNewGame()
    {
        Debug.Log("New Game Clicked: 기존 데이터 삭제 및 인트로 시작");

        // 1. 기존 세이브 파일 삭제 (완전 초기화)
        // 삭제를 안 하면 인트로 보고 게임 씬 갔을 때 옛날 데이터가 로드될 수 있음
        if (File.Exists(SavePath))
        {
            File.Delete(SavePath);
        }

        // 2. 인트로 씬 로드
        SceneManager.LoadScene(introSceneName);
    }

    // 이어하기
    public void OnClickContinue()
    {
        // 파일이 실제로 있는지 한 번 더 확인
        if (File.Exists(SavePath))
        {
            Debug.Log("Continue Clicked: 게임 씬으로 이동하여 로드 진행");

            // 게임 씬으로 바로 이동
            // (GameManager가 Start()에서 파일을 감지하고 자동으로 로드함)
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