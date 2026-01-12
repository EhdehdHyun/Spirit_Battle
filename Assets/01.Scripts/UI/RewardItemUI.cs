using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RewardItemUI : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text amountText;

    public void Set(Sprite sprite, int amount)
    {
        icon.sprite = sprite;
        amountText.text = amount.ToString();
    }
}