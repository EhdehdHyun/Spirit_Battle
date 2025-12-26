using UnityEngine;

public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance;

    [SerializeField] private TutorialUI tutorialUI;
    private bool hasShownMoveGuide = false;

    bool w, a, s, d;
    bool isActive;

    private void Awake()
    {
        Instance = this;
        tutorialUI.Hide();
    }

    // 대화 끝났을 때 호출할 함수
    public void StartMoveTutorial()
    {
        Debug.Log("StartMoveTutorial CALLED");
        // 저장 기준 1회 제한 
        //if (PlayerPrefs.GetInt("MoveGuideShown", 0) == 1)
            //return;

        // 실행 중 1회 제한
        if (hasShownMoveGuide)
            return;

        hasShownMoveGuide = true;
        isActive = true;
        w = a = s = d = false;

        tutorialUI.Show("WASD 키로 움직여 보세요");

        PlayerPrefs.SetInt("MoveGuideShown", 1);
        PlayerPrefs.Save();
    }

    private void Update()
    {
        if (!isActive) return;

        if (Input.GetKeyDown(KeyCode.W)) w = true;
        if (Input.GetKeyDown(KeyCode.A)) a = true;
        if (Input.GetKeyDown(KeyCode.S)) s = true;
        if (Input.GetKeyDown(KeyCode.D)) d = true;

        if (w && a && s && d)
        {
            CompleteMoveTutorial();
        }
    }

    void CompleteMoveTutorial()
    {
        isActive = false;
        tutorialUI.Hide();

        // 여기서 다음 단계 호출
        StartJumpTutorial();
    }

    void StartJumpTutorial()
    {
        tutorialUI.Show("Space 키로 점프해 보세요.");
        // 점프 입력 체크는 여기서 이어서 구현
    }
}