using UnityEngine;
using TMPro;


public class QuestTrackerUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI progressText;

    public void SetProgress(int current, int required)
    {
        progressText.text = $"진행도 ( {current} / {required} )";
    }

    public void ResetProgress()
    {
        progressText.text = "진행도 ( 0 / 0 )";
    }
}
