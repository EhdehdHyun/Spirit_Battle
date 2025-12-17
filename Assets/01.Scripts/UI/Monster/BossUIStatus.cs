using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BossUIStatus : MonoBehaviour
{
    public static BossUIStatus Instance { get; private set; }

    [Header("Boss HP Bar")]
    public Image hpBar;

    [Header("Boss Text")]
    public TMP_Text hpText;
    public TMP_Text bossNameText;

    //일단 혹시 몰라서 페이즈 수를 받는 변수를 선언하긴 했는데 안 쓰면 지울 예정
    public TMP_Text phaseText;

    private BossEnemy boss;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;

        SetVisible(false);
    }

    public void SetBoss(BossEnemy bossEnemy)
    {
        boss = bossEnemy;

        if (boss == null)
        {
            SetVisible(false);
            return;
        }

        SetVisible(true);
        RefreshAll();
    }

    public void UpdateHp(float current, float max)
    {
        if (hpBar != null)
            hpBar.fillAmount = max > 0f ? current / max : 0f;
        if (hpText != null)
            hpText.text = $"{Mathf.FloorToInt(current)} / {Mathf.FloorToInt(max)}";
    }

    public void UpdatePhase(int phase)
    {
        if (phaseText != null)
            phaseText.text = $"Phase {phase}";
    }

    public void UpdateBossName(string name)
    {
        if (bossNameText != null)
            bossNameText.text = name;
    }

    public void RefreshAll()
    {
        if (boss == null) return;

        UpdateHp(boss.currentHp, boss.maxHp);
        UpdatePhase(boss.CurrentPhase);
        UpdateBossName(boss.name);
    }

    public void SetVisible(bool visible)
    {
        gameObject.SetActive(visible);
    }
}
