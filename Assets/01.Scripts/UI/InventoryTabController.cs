using UnityEngine;
using UnityEngine.UI;

public class InventoryTabController : MonoBehaviour
{
    [Header("인벤토리 오픈시 상태창 x")]
    [SerializeField] private GameObject statusCanvas; //항상 켜져 있던 Canvas
    
    [Header("탭 버튼 (아이콘)")]
    [SerializeField] private Button weaponTabButton; // 검 아이콘 버튼
    [SerializeField] private Button itemTabButton;   // 빨간 아이콘 버튼

    [Header("패널")]
    [SerializeField] private GameObject weaponPanel; 
    [SerializeField] private GameObject itemPanel;

    private void Start()
    {
        // 버튼 클릭 이벤트 연결
        weaponTabButton.onClick.AddListener(ShowWeaponPanel);
        itemTabButton.onClick.AddListener(ShowItemPanel);

        // 처음엔 무기 패널만 보이게
        ShowWeaponPanel();
    }
    
    

    private void ShowWeaponPanel()
    {
        weaponPanel.SetActive(true);
        itemPanel.SetActive(false);
    }

    private void ShowItemPanel()
    {
        weaponPanel.SetActive(false);
        itemPanel.SetActive(true);
    }
    
    public void OpenInventory()
    {
        this.gameObject.SetActive(true);   // PageCanvas 켜기
        statusCanvas.SetActive(false);     // 상태창 끄기
    }
    public void CloseInventory()
    {
        this.gameObject.SetActive(false);
        // PageCanvas 비활성화
        transform.parent.parent.gameObject.SetActive(false);
    }
}