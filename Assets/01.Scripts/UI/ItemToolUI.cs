using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ItemTooltipUI : MonoBehaviour
{
    public static ItemTooltipUI Instance { get; private set; }

    [Header("루트 패널 (툴팁 전체)")]
    [SerializeField] private GameObject root;   // 비워두면 this.gameObject 사용

    [Header("UI 참조")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI quantityText;

    private CanvasGroup canvasGroup;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (root == null)
            root = this.gameObject;

        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            // 툴팁이 마우스 클릭을 막지 않도록
            canvasGroup.blocksRaycasts = false;
        }

        Debug.Log("[ItemTooltipUI] Awake 호출, Instance 설정 완료");
        Hide();
    }

    public void Show(Data_table data, int quantity)
    {
        if (data == null)
            return;

        Debug.Log($"[ItemTooltipUI] Show: {data.ItemName}, x{quantity}");

        if (root != null && !root.activeSelf)
            root.SetActive(true);

        // 아이콘
        if (iconImage != null)
        {
            Sprite iconSprite = null;
            if (!string.IsNullOrEmpty(data.Icon))
            {
                iconSprite = Resources.Load<Sprite>($"ItemIcons/{data.Icon}");
            }

            if (iconSprite != null)
            {
                iconImage.sprite = iconSprite;
                iconImage.enabled = true;
                iconImage.color = Color.white;
            }
            else
            {
                iconImage.enabled = false;
            }
        }

        // 이름
        if (nameText != null)
            nameText.text = data.ItemName;

        // 수량
        if (quantityText != null)
        {
            if (quantity > 1)
            {
                quantityText.text = $"x{quantity}";
                quantityText.enabled = true;
            }
            else
            {
                quantityText.text = "";
                quantityText.enabled = false;
            }
        }

        // (선택) 마우스 근처로 위치 이동
        RectTransform rt = root.GetComponent<RectTransform>();
        if (rt != null && rt.parent is RectTransform parentRt)
        {
            Vector2 localPos;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentRt,
                Input.mousePosition,
                null,
                out localPos
            );
            rt.anchoredPosition = localPos + new Vector2(10f, 10f);
        }
    }

    public void Hide()
    {
        if (root != null && root.activeSelf)
            root.SetActive(false);
    }
}
