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

    [Header("Stamina Charge")]
    public float regenPerSecond = 1f;
    private float regenTimer = 0f;

    [Header("Dash Count")]
    public int maxDashCount = 2;
    public int curDashCount;

    [Header("Run Stamina (전투 중)")]
    public float combatRunStaminaPerSec = 10f;

    [Header("Dash Stamina")]
    public float dashUseStaminaCost = 15f;

    [Header("Dash Burst")]
    public float secondDashWindow = 0.45f;   // 일정 시간
    public float secondDashCooldown = 1f;

    private float _secondWindowRemain = 0f;
    private bool _secondDashUsed = false;
    private float _dashCooldownRemain = 0f;

    public float maxExp; // 다음 레벨 필요 경험치

    private CharacterBase character;
    private Level_Data_Loader levelTable;

    public bool IsDashCooling => _dashCooldownRemain > 0f;
    public bool CanSecondDashNow => (_secondWindowRemain > 0f) && !_secondDashUsed;

    private void Awake()
    {
        character = GetComponent<CharacterBase>();
        levelTable = new Level_Data_Loader();
    }

    private void Start()
    {
        ApplyLevelData();
        UpdateAllUI();
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
        float dt = Time.deltaTime;

        AutoRegenStamina(dt);

        if (_dashCooldownRemain > 0f)
        {
            _dashCooldownRemain -= dt;
            if (_dashCooldownRemain < 0f) _dashCooldownRemain = 0f;
        }

        // 2번째 대쉬 입력 윈도우
        if (_secondWindowRemain > 0f)
        {
            _secondWindowRemain -= dt;
            if (_secondWindowRemain <= 0f)
            {
                // 2번째 안 썼으면 종료
                _secondWindowRemain = 0f;
                _secondDashUsed = false;
            }
        }
    }

    void AutoRegenStamina(float dt)
    {
        if (maxStamina <= 0f) return;

        // 이미 풀스태면 타이머 정리
        if (currentStamina >= maxStamina)
        {
            currentStamina = maxStamina;
            regenTimer = 0f;
            return;
        }

        regenTimer += dt;

        float before = currentStamina;

        while (regenTimer >= 1f)
        {
            regenTimer -= 1f;
            currentStamina = Mathf.Min(maxStamina, currentStamina + regenPerSecond);
            if (currentStamina >= maxStamina) { regenTimer = 0f; break; }
        }

        if (!Mathf.Approximately(before, currentStamina))
            ui?.UpdateStamina(currentStamina, maxStamina);

    }

    public bool CanStartDashUse()
    {
        if (IsDashCooling) return false;
        if (_secondWindowRemain > 0f) return false;
        return currentStamina >= dashUseStaminaCost;
    }

    public bool CommitDashUseStart()
    {
        if (!CanStartDashUse()) return false;
        if (!TryConsumeStamina(dashUseStaminaCost)) return false;

        _secondDashUsed = false;
        _secondWindowRemain = secondDashWindow;
        return true;
    }

    public void CommitSecondDashUsed()
    {
        if (!CanSecondDashNow) return;

        _secondDashUsed = true;
        _secondWindowRemain = 0f;
        _dashCooldownRemain = secondDashCooldown;
    }

    public bool HasStamina(float amount) => currentStamina >= amount;

    public bool TryConsumeStamina(float amount)
    {
        if (amount <= 0f) return true;
        if (currentStamina < amount) return false;

        currentStamina -= amount;
        if (currentStamina < 0f) currentStamina = 0f;

        ui?.UpdateStamina(currentStamina, maxStamina);
        return true;
    }

    public bool TickCombatRunStamina(bool inCombat, bool isTryingRun, float dt)
    {
        if (!inCombat) return true;
        if (!isTryingRun) return true;

        float cost = combatRunStaminaPerSec * dt;
        if (!TryConsumeStamina(cost))
        {
            currentStamina = 0f;
            ui?.UpdateStamina(currentStamina, maxStamina);
            return false;
        }

        return currentStamina > 0f;
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
