using UnityEngine;
using TMPro;

public class ItemTooltipUI : MonoBehaviour
{
    public static ItemTooltipUI Instance { get; private set; }

    [Header("Tooltip UI Root")]
    [Tooltip("툴팁 전체 패널 (비활성/활성 토글용)")]
    public GameObject root;

    [Header("Text References")]
    public TextMeshProUGUI titleText;  // 아이템 이름
    public TextMeshProUGUI bodyText;   // 설명/세부정보

    [Header("Position")]
    [Tooltip("마우스 기준 오프셋")]
    public Vector2 offset = new Vector2(16f, -16f);

    private RectTransform _rectTransform;
    private Canvas _canvas;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        _canvas = GetComponentInParent<Canvas>();

        if (root != null)
            _rectTransform = root.GetComponent<RectTransform>();
        else
            _rectTransform = GetComponent<RectTransform>();

        Hide();
    }

    public void Show(ItemInstance itemInstance, Vector2 screenPosition)
    {
        if (itemInstance == null || itemInstance.data == null || root == null)
            return;

        var data = itemInstance.data;

        if (titleText != null)
            titleText.text = data.ItemName;

        if (bodyText != null)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();

            sb.AppendLine($"종류 : {data.ItemType}");
            sb.AppendLine($"등급 : {data.Rarity}");

            if (data.IsConsumable)
            {
                if (data.HealAmount > 0)
                    sb.AppendLine($"사용 시 HP {data.HealAmount} 회복");
                else
                    sb.AppendLine("사용 가능한 소모품");
            }
            else
            {
                sb.AppendLine("소모되지 않는 아이템");
            }

            sb.AppendLine($"최대 스택 : {data.MaxStack}");


            bodyText.text = sb.ToString();
        }

        UpdatePosition(screenPosition);
        root.SetActive(true);
    }

    public void Hide()
    {
        if (root != null)
            root.SetActive(false);
    }

    public void UpdatePosition(Vector2 screenPosition)
    {
        if (_rectTransform == null)
            return;

        Vector2 pos = screenPosition + offset;

        _rectTransform.position = pos;
    }
}
