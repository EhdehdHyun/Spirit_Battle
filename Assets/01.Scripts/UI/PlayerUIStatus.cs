using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerUIStatus : MonoBehaviour
{
    [Header("Bars")]
    public Image hpBar;
    public Image staminaBar;
    public Image expBar;

    [Header("Bars의Text")]
    public TMP_Text hpText;
    public TMP_Text staminaText;
    public TMP_Text expText;
    public TMP_Text levelText;

    public void UpdateHp(float current, float max)
    {
        hpBar.fillAmount = current / max;
        if (hpText != null)
        {
            hpText.text = $"{Mathf.FloorToInt(current)} / {Mathf.FloorToInt(max)}";
        }
    }
    // 스태미나 갱신
    public void UpdateStamina(float current, float max)
    {
        staminaBar.fillAmount = current / max;
        if (staminaText != null)
            staminaText.text = $"{Mathf.FloorToInt(current)} / {Mathf.FloorToInt(max)}";
    }

    // EXP 갱신
    public void UpdateExp(float current, float max)
    {
        expBar.fillAmount = current / max;
        if (expText != null)
            expText.text = $"{Mathf.FloorToInt(current)} / {Mathf.FloorToInt(max)}";
    }

    // 레벨 갱신
    public void UpdateLevel(int level)
    {
        if (levelText != null)
            levelText.text = $"Lv.{level}";
    }
}
