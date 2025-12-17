using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.VisualScripting;

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
    [Header("Boss Break Bar")]
    [SerializeField] private Image breakBar;
    [Tooltip("BreakBar가 목표값을 따라가는 속도 (쭉~ 차오르는 느낌)")]
    [Range(1f, 15f)]
    [SerializeField] private float breakSmoothSpeed = 6f;

    private float breakTargetFill = 1f;

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

        if (breakBar != null)
        {
            breakBar.fillAmount = 1f;
            breakBar.gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        //breakBar가 부드럽게 차게 보정
        if (breakBar != null)
            breakBar.fillAmount = Mathf.Lerp(breakBar.fillAmount, breakTargetFill, Time.deltaTime * breakSmoothSpeed);
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

    //브레이크 UI 
    public void UpdateBreak(int currentHHits, int thereshold)
    {
        if (breakBar == null) return;
        if (thereshold <= 0)
        {
            breakTargetFill = 1f;
            return;
        }

        breakTargetFill = Mathf.Clamp01(1f - (float)currentHHits / thereshold);
    }

    //그로기 시작/종료에 따른 게이지 처리
    public void SetGroggy(bool isGroggy)
    {
        if (breakBar == null) return;

        breakTargetFill = isGroggy ? 0f : 1f;
    }

    public void SetBreakVisible(bool visible)
    {
        if (breakBar != null)
            breakBar.gameObject.SetActive(visible);
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
