using UnityEngine;

public class TutorialManager : MonoBehaviour
{
    enum Step { Move, Jump, Done }
    Step currentStep;

    public static TutorialManager Instance;

    [SerializeField] private TutorialUI tutorialUI;
    [SerializeField] WorldArrowController worldArrow;


    bool w, a, s, d;
    bool isActive;
    bool hasShownMoveGuide;

    private void Awake()
    {
        Instance = this;
        tutorialUI.Hide();
    }

    public void StartMoveTutorial()
    {
        if (hasShownMoveGuide) return;

        hasShownMoveGuide = true;
        isActive = true;
        currentStep = Step.Move;

        w = a = s = d = false;
        tutorialUI.Show("WASD 키로 움직여 보세요");
    }

    private void Update()
    {
        if (!isActive) return;

        if (currentStep == Step.Move)
        {
            if (Input.GetKeyDown(KeyCode.W)) w = true;
            if (Input.GetKeyDown(KeyCode.A)) a = true;
            if (Input.GetKeyDown(KeyCode.S)) s = true;
            if (Input.GetKeyDown(KeyCode.D)) d = true;

            if (w && a && s && d)
                CompleteMoveTutorial();
        }
        else if (currentStep == Step.Jump)
        {
            if (Input.GetKeyDown(KeyCode.Space))
                CompleteJumpTutorial();
        }
    }

    void CompleteMoveTutorial()
    {
        currentStep = Step.Jump;
        tutorialUI.Show("Space 키로 점프해 보세요.");
    }

    void CompleteJumpTutorial()
    {
        isActive = false;
        currentStep = Step.Done;

        tutorialUI.Hide();
        tutorialUI.Show("화살표 방향으로 이동해 보세요.");

        // 다음 단계: 월드 화살표 + 이동 트리거 ON
        
        worldArrow.gameObject.SetActive(true); 
    }
}