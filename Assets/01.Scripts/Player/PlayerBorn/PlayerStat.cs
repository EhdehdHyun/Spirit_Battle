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

    public float maxExp; // 다음 레벨 필요 경험치

    private CharacterBase character;
    private Level_Data_Loader levelTable;

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
}
