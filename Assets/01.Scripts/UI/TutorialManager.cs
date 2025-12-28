using UnityEngine;

public class TutorialManager : MonoBehaviour
{
    enum Step
    {
        Move, 
        Jump,
        Dash,
        Attack,
        Done
    }
    Step currentStep;

    public static TutorialManager Instance;

    [SerializeField] private TutorialUI tutorialUI;
    [SerializeField] private WorldArrowController worldArrow;
    [SerializeField] private Transform attackTargetMonster;
    
    bool isActive;
    bool hasShownMoveGuide;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        tutorialUI.Hide();
    }

    public void StartMoveTutorial()
    {
        if (hasShownMoveGuide) return;

        hasShownMoveGuide = true;
        isActive = true;
        currentStep = Step.Move;
        
        tutorialUI.Show("WASD 키로 움직여 보세요");
    }

    private void Update()
    {
        if (!isActive) return;

        if (currentStep == Step.Move)
        {
            if (
                Input.GetKeyDown(KeyCode.W) ||
                Input.GetKeyDown(KeyCode.A) ||
                Input.GetKeyDown(KeyCode.S) ||
                Input.GetKeyDown(KeyCode.D)
            )
            {
                CompleteMoveTutorial();
            }
        }
        else if (currentStep == Step.Jump)
        {
            if (Input.GetKeyDown(KeyCode.Space))
                CompleteJumpTutorial();
        }
        else if (currentStep == Step.Dash)
        {
            if (Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift))
            {
                CompleteDashTutorial();
            }
        }
        else if (currentStep == Step.Attack)
        {
            // 우클릭 : 무기 꺼내기
            if (Input.GetMouseButtonDown(1))
            {
                Debug.Log("Weapon Draw Input");
            }

            // 좌클릭 : 공격
            if (Input.GetMouseButtonDown(0))
            {
                CompleteAttackTutorial();
            }
        }
       
    }

    void CompleteMoveTutorial()
    {
        currentStep = Step.Jump;
        tutorialUI.Show("Space 키로 점프해 보세요.");
    }

    void CompleteJumpTutorial()
    {
        currentStep = Step.Dash;
        tutorialUI.Show("Shift 키를 눌러 대쉬를 사용할 수 있습니다!");
    }
    void CompleteDashTutorial()
    {
        currentStep = Step.Done;
        isActive = false;

        tutorialUI.Show("화살표 방향으로 이동해 F키를 눌러 보세요.");
        worldArrow.gameObject.SetActive(true);
    }
    public void ShowMoveForwardText()
    {
        tutorialUI.Show("Tab키를 눌러 장비칸에서 무기를 클릭해 장착한 뒤 화살표 방향으로 이동하세요");
        worldArrow.SetTarget(attackTargetMonster);
    }
    
    public void StartAttackTutorial()
    {
        worldArrow.ClearTarget();
        isActive = true;
        currentStep = Step.Attack;

        tutorialUI.Show(
            "오른쪽 클릭으로 검을 꺼내고\n왼쪽 클릭으로 공격을 시도해 보세요!"
        );
    }
    void CompleteAttackTutorial()
    {
        isActive = false;
        currentStep = Step.Done;

        tutorialUI.Hide();
        Debug.Log("Attack Tutorial Complete");
    }
    public void ShowSimpleMessage(string message)
    {
        tutorialUI.Show("다음 몬스터를 상대하면서 패링을 배워보자");
    }
}