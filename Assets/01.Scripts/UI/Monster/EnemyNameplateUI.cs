using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class EnemyNameplateUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private Image hpFill;

    [Header("Target")]
    [SerializeField] private CharacterBase target;
    [SerializeField] private string nameOverride;

    [Header("옵션")]
    [SerializeField] private bool hideWhenDead = true;

    private Coroutine refreshCo;

    private void Awake()
    {
        if (target == null)
            target = GetComponentInParent<CharacterBase>();

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
        refreshCo = null;
    }

    public void SetTarget(CharacterBase newTarget, string overrideName = null)
    {
        Unbind();

        target = newTarget;
        nameOverride = overrideName ?? string.Empty;

        ApplyName();
        Bind();

        if (refreshCo != null)
            StopCoroutine(refreshCo);

        refreshCo = StartCoroutine(DelayedRefresh());
    }

    private void Bind()
    {
        if (target == null) return;
        target.OnHpChanged -= HandleHpChanged;
        target.OnHpChanged += HandleHpChanged;
    }

    private void Unbind()
    {
        if (target == null) return;
        target.OnHpChanged -= HandleHpChanged;
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
}
