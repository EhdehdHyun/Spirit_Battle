using UnityEngine;
using UnityEngine.VFX;

public class PlayerWeaponVisual : MonoBehaviour
{
    [SerializeField] private InventoryManager inventoryManager;
    [SerializeField] private GameObject swordObject;

    private static readonly string[] AUTO_PLAY_VFX_NAMES = { "vfxgraph_Slash", "vfxgraph_Spear" };

    private void Awake()
    {
        if (inventoryManager == null)
            inventoryManager = InventoryManager.Instance;
    }

    private void OnEnable()
    {
        if (inventoryManager != null)
            inventoryManager.OnInventoryChanged += Refresh;
    }

    private void OnDisable()
    {
        if (inventoryManager != null)
            inventoryManager.OnInventoryChanged -= Refresh;
    }

    private void Update()
    {
        Refresh();
    }

    private void Refresh()
    {
        if (inventoryManager == null || swordObject == null) return;

        bool hasItem = inventoryManager.GetEquippedWeapon() != null;

        // 장착만 되어 있다면 무조건 활성화 (허리에 보이게 됨)
        if (swordObject.activeSelf != hasItem)
        {
            swordObject.SetActive(hasItem);

            // 무기가 켜질 때 쓸데없는 이펙트(검기 등)가 나오지 않도록 끔
            if (hasItem)
                DisableAutoPlayVFXGraphsOnly(swordObject.transform);
        }
    }

    private void DisableAutoPlayVFXGraphsOnly(Transform weaponRoot)
    {
        foreach (Transform t in weaponRoot.GetComponentsInChildren<Transform>(true))
        {
            for (int i = 0; i < AUTO_PLAY_VFX_NAMES.Length; i++)
            {
                if (t.name == AUTO_PLAY_VFX_NAMES[i])
                {
                    var vfx = t.GetComponent<VisualEffect>();
                    if (vfx != null) vfx.Stop();
                    t.gameObject.SetActive(false);
                }
            }
        }
    }
}