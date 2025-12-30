using UnityEngine;
using UnityEngine.SceneManagement;

public class TitleMenuController : MonoBehaviour
{
    [Header("Scene Names")]
    [SerializeField] private string introSceneName = "IntroScene";

    // 새로 시작
    public void OnClickNewGame()
    {
        Debug.Log("New Game Clicked");

        // 나중에 세이브 초기화 여기서
        // SaveManager.Instance.Reset();

        SceneManager.LoadScene(introSceneName);
    }

    // 이어하기 (아직 미구현)
    public void OnClickContinue()
    {
        Debug.Log("Continue Clicked (Not Implemented)");
    }

    // 옵션 (아직 미구현)
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