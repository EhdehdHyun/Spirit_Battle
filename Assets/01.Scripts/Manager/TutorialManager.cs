using UnityEngine;
using System.Collections;
using TMPro;

public enum TutorialStep
{
    None,
    MoveInfo,
    JumpInfo,
    MapInfo,
    End
}

public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance;

    [Header("UI")]
    [SerializeField] private GameObject tutorialUI;
    [SerializeField] private TextMeshProUGUI tutorialText;

    private TutorialStep currentStep = TutorialStep.None;

    private void Awake()
    {
        Debug.Log(" TutorialManager Awake");

        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    private void Update()
    {
        if (currentStep == TutorialStep.None) return;

        if (Input.GetKeyDown(KeyCode.F))
        {
            NextStep();
        }
    }
    public void StartTutorialDelayed()
    {
        StartCoroutine(StartTutorialNextFrame());
    }

    private IEnumerator StartTutorialNextFrame()
    {
        yield return null; // 1프레임 대기
        StartTutorial();
    }

    public void StartTutorial()
    {
        tutorialUI.SetActive(true);
        //Time.timeScale = 0f;                 // 입력 차단
        SetStep(TutorialStep.MoveInfo);
    }

    private void NextStep()
    {
        switch (currentStep)
        {
            case TutorialStep.MoveInfo:
                SetStep(TutorialStep.JumpInfo);
                break;

            case TutorialStep.JumpInfo:
                SetStep(TutorialStep.MapInfo);
                break;

            case TutorialStep.MapInfo:
                SetStep(TutorialStep.End);
                break;
        }
    }

    private void SetStep(TutorialStep step)
    {
        currentStep = step;

        switch (step)
        {
            case TutorialStep.MoveInfo:
                tutorialText.text =
                    "W A S D 키로 이동할 수 있습니다.";
                break;

            case TutorialStep.JumpInfo:
                tutorialText.text =
                    "Space Bar로 장애물을 뛰어넘을 수 있습니다.";
                break;

            case TutorialStep.MapInfo:
                tutorialText.text =
                    "M 키를 눌러 지도를 열 수 있습니다.";
                break;

            case TutorialStep.End:
                EndTutorial();
                break;
        }
    }

    private void EndTutorial()
    {
        tutorialUI.SetActive(false);
        currentStep = TutorialStep.None;
       // Time.timeScale = 1f; //  게임 재개
    }
}
