using UnityEngine;

public class PlayerStat : MonoBehaviour
{
    [SerializeField] private PlayerUIStatus ui;
    private CharacterBase character;   

    public int level = 1;
    
    public float maxStamina = 100f;
    public float currentStamina = 100f;

    public float maxExp = 100f;
    public float currentExp = 0f;

    private void Awake()
    {
        character = GetComponent<CharacterBase>();
    }

    private void Start()
    {
        UpdateAllUI();    
    }

    private void Update()
    {
        ui.UpdateHp(character.currentHp, character.maxHp);
        ui.UpdateStamina(currentStamina, maxStamina);
        ui.UpdateExp(currentExp, maxExp);
        ui.UpdateLevel(level);
    }

    private void UpdateHpUI()
    {
        if (character == null) return;
        ui.UpdateHp(character.currentHp, character.maxHp);
    }

    private void UpdateAllUI()
    {
        ui.UpdateHp(character.currentHp, character.maxHp);
        ui.UpdateLevel(level);       
    }
}