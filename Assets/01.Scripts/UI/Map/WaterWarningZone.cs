using UnityEngine;

public class WaterWarningZone : MonoBehaviour
{
    [Header("연결할 UI")]
    [SerializeField] private GameObject warningUIPanel;

    [Header("설정")]
    [SerializeField] private string playerTag = "Player";

    private void Start()
    {
        if (warningUIPanel != null)
        {
            warningUIPanel.SetActive(false);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            if (warningUIPanel != null)
                warningUIPanel.SetActive(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            if (warningUIPanel != null)
                warningUIPanel.SetActive(false);
        }
    }
}