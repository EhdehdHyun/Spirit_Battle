using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class EnemyNameplateUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private Image hpFill;
    [SerializeField] private Image breakFill;

    [Header("Target")]
    [SerializeField] private CharacterBase target;
    [SerializeField] private string nameOverride;

    [Header("옵션")]
    [SerializeField] private bool hideWhenDead = true;

    private Coroutine refreshCo;
    private EnemyBase enemy;

    private void Awake()
    {
        if (target == null)
            target = GetComponentInParent<CharacterBase>();

        enemy = target as EnemyBase;
        ApplyName();
    }
    private void OnEnable()
    {
        Bind();

        if (refreshCo != null)
            StopCoroutine(refreshCo);

        refreshCo = StartCoroutine(DelayedRefresh());
    }

    private void OnDisable()
    {
        Unbind();

        if (refreshCo != null)
        {
            StopCoroutine(refreshCo);
            refreshCo = null;
        }
    }

    private void OnDestroy()
    {
        Unbind();
    }

    private IEnumerator DelayedRefresh()
    {
        yield return null;

        RefreshHp();

        if (enemy != null)
            HandleBreakChanged(enemy.BreakHitCount, enemy.BreakHitThreshold);
        else
            HandleBreakChanged(0, 1);

        refreshCo = null;
    }

    public void SetTarget(CharacterBase newTarget, string overrideName = null)
    {
        Unbind();

        target = newTarget;
        enemy = target as EnemyBase;
        nameOverride = overrideName ?? string.Empty;

        ApplyName();
        Bind();

        if (refreshCo != null)
            StopCoroutine(refreshCo);

        refreshCo = StartCoroutine(DelayedRefresh());
    }

    private void Bind()
    {
        if (enemy != null)
            HandleBreakChanged(enemy.BreakHitCount, enemy.BreakHitThreshold);
        else
            HandleBreakChanged(0, 1);

        if (target == null) return;

        target.OnHpChanged -= HandleHpChanged;
        target.OnHpChanged += HandleHpChanged;

        enemy = target as EnemyBase;
        if (enemy != null)
        {
            enemy.OnBreakHitChanged -= HandleBreakChanged;
            enemy.OnBreakHitChanged += HandleBreakChanged;

            HandleBreakChanged(enemy.BreakHitCount, enemy.BreakHitThreshold);
        }
    }

    private void Unbind()
    {
        if (target == null) return;

        target.OnHpChanged -= HandleHpChanged;

        if (enemy != null)
        {
            enemy.OnBreakHitChanged -= HandleBreakChanged;
            enemy = null;
        }
    }

    private void ApplyName()
    {
        if (nameText == null) return;

        if (!string.IsNullOrEmpty(nameOverride))
            nameText.text = nameOverride;
        else if (target != null)
            nameText.text = target.gameObject.name;
    }

    private void RefreshHp()
    {
        if (target == null) return;
        HandleHpChanged(target.currentHp, target.maxHp);
    }

    private void HandleHpChanged(float current, float max)
    {
        if (hpFill != null)
            hpFill.fillAmount = (max > 0f) ? Mathf.Clamp01(current / max) : 0f;

        if (hideWhenDead && current <= 0f)
            gameObject.SetActive(false);
    }

    private void HandleBreakChanged(int current, int max)
    {
        if (breakFill == null) return;

        float t = (max > 0) ? Mathf.Clamp01((float)current / max) : 0f;

        breakFill.fillAmount = 1f - t;
    }
}
