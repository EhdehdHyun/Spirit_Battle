using UnityEngine;
using TMPro;

public enum TutorialStep
{
    None,
    Move,
    OpenMap,
    End
}

public class TutorialManager : MonoBehaviour
{public static TutorialManager Instance;

    [Header("UI")]
    [SerializeField] private GameObject tutorialUI;
    [SerializeField] private TextMeshProUGUI tutorialText;

    private TutorialStep currentStep = TutorialStep.None;

    private void Awake()
    {
        Debug.Log("🟢 TutorialManager Awake");
       if (Instance == null)
           Instance = this;
        else
            Destroy(gameObject);
    }

   private void Update()
   {
        if (currentStep == TutorialStep.None) return;

        switch (currentStep)
        {
           case TutorialStep.Move:
                CheckMoveInput();
                break;

            case TutorialStep.OpenMap:
                CheckMapInput();
                break;
        } 
   }

    // NPC 대사 끝나면 여기 호출
    public void StartTutorial()
    {
        tutorialUI.SetActive(true);
        SetStep(TutorialStep.Move);
    }

    private void SetStep(TutorialStep step)
    {
        currentStep = step;

        switch (step)
        {
            case TutorialStep.Move:
                tutorialText.text = "W A S D 또는 방향키로 이동해 보세요";
                break;

            case TutorialStep.OpenMap:
                tutorialText.text = "M 키를 눌러 지도를 열어보세요";
                break;

            case TutorialStep.End:
                EndTutorial();
                break;
        }
    }

    private void CheckMoveInput()
    {
        if (Input.GetAxisRaw("Horizontal") != 0 ||
            Input.GetAxisRaw("Vertical") != 0)
        {
            SetStep(TutorialStep.OpenMap);
        }
    }

    private void CheckMapInput()
    {
        if (Input.GetKeyDown(KeyCode.M))
        {
            SetStep(TutorialStep.End);
        }
    }

    private void EndTutorial()
    {
        tutorialUI.SetActive(false);
        currentStep = TutorialStep.None;
    }
}