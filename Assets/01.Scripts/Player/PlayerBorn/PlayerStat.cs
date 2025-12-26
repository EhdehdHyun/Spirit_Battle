using UnityEngine;

public class PlayerStat : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private PlayerUIStatus ui;

    [Header("Level")]
    public int level = 1;
    public float currentExp = 0f;

    [Header("Stats")]
    public float maxStamina;
    public float currentStamina;

    [Header("Dash Count")]
    public int maxDashCount = 2;
    public int curDashCount;

    [Header("Dash Charge Count")]
    public float dashRechargeTime = 1f;

    private float dashRechargeAcc = 0f;

    public float maxExp; // 다음 레벨 필요 경험치

    private CharacterBase character;
    private Level_Data_Loader levelTable;

    private void Awake()
    {
        character = GetComponent<CharacterBase>();
        levelTable = new Level_Data_Loader();
        curDashCount = maxDashCount;
    }

    private void Start()
    {
        ApplyLevelData();
        UpdateAllUI();

        dashRechargeAcc = 0f;
    }

    private void OnEnable()
    {
        if (character != null)
            character.OnHpChanged += HandleHpChanged;
    }

    private void OnDisable()
    {
        if (character != null)
            character.OnHpChanged -= HandleHpChanged;
    }

    private void HandleHpChanged(float current, float max)
    {
        if (ui != null)
            ui.UpdateHp(current, max);
    }

    private void Update()
    {
        RechargeDashCount();
    }

    public bool TryConsumeDash()
    {
        if (curDashCount <= 0) return false;

        curDashCount--;
        return true;
    }

    private void RechargeDashCount()
    {
        if (curDashCount >= maxDashCount) return;

        dashRechargeAcc += Time.deltaTime;

        while (dashRechargeAcc >= dashRechargeTime && curDashCount < maxDashCount)
        {
            dashRechargeAcc -= dashRechargeTime;
            curDashCount++;

            // ui.UpdateDash(curDashCount, maxDashCount);
        }

        if (curDashCount >= maxDashCount)
            dashRechargeAcc = 0f;
    }

    // =======================
    // 레벨 데이터 적용
    // =======================
    private void ApplyLevelData()
    {
        var data = levelTable.GetByLevel(level);
        if (data == null)
        {
            Debug.LogWarning($"Level data not found : {level}");
            return;
        }

        Debug.Log($"[PlayerStat] Apply Level {level} | HP:{data.MaxHP} | Stamina:{data.Stamina}");

        // HP
        character.maxHp = data.MaxHP;
        character.currentHp = character.maxHp;

        // Stamina
        maxStamina = data.Stamina;
        currentStamina = maxStamina;

        // EXP
        var nextLevel = levelTable.GetByLevel(level + 1);
        maxExp = nextLevel != null ? nextLevel.RequiredExp : 0;
    }

    // =======================
    // 경험치 추가
    // =======================
    public void AddExp(float amount)
    {
        if (maxExp <= 0) return; // MaxLevel

        currentExp += amount;

        if (currentExp >= maxExp)
        {
            currentExp -= maxExp;
            LevelUp();
        }

        UpdateExpUI();
    }

    private void LevelUp()
    {
        level++;
        ApplyLevelData();
        character.currentHp = character.maxHp;

        UpdateAllUI();

        Debug.Log($"LEVEL UP → {level}");
    }

    // =======================
    // UI
    // =======================
    private void UpdateAllUI()
    {
        ui.UpdateHp(character.currentHp, character.maxHp);
        ui.UpdateStamina(currentStamina, maxStamina);
        ui.UpdateExp(currentExp, maxExp);
        ui.UpdateLevel(level);
    }

    private void UpdateExpUI()
    {
        ui.UpdateExp(currentExp, maxExp);
        ui.UpdateLevel(level);
    }

    // =======================
    // 체력회복
    // =======================
    public bool TryHeal(int amount)
    {
        if (amount <= 0) return false;
        if (character == null) return false;

        float before = character.currentHp;
        character.currentHp = Mathf.Min(character.maxHp, character.currentHp + amount);

        // 체력이 이미 풀이라 변화가 없으면 false
        if (Mathf.Approximately(character.currentHp, before))
            return false;

        if (ui != null)
            ui.UpdateHp(character.currentHp, character.maxHp);

        return true;
    }
}
