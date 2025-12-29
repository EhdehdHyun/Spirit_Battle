using UnityEngine;
using UnityEngine.VFX;

public class PlayerWeaponVisual : MonoBehaviour
{
    [SerializeField] private InventoryManager inventoryManager;
    [SerializeField] private GameObject swordObject; // Sword_001 or Sword_002

    // ✅ 소켓(VFX_Slash)은 끄면 안됨! vfxgraph만 끈다
    private static readonly string[] AUTO_PLAY_VFX_NAMES =
    {
        "vfxgraph_Slash",
        "vfxgraph_Spear"
    };

    private void Awake()
    {
        if (inventoryManager == null)
            inventoryManager = InventoryManager.Instance;
    }

    private void OnEnable()
    {
        if (inventoryManager != null)
            inventoryManager.OnInventoryChanged += Refresh;

        Refresh();
    }

    private void OnDisable()
    {
        if (inventoryManager != null)
            inventoryManager.OnInventoryChanged -= Refresh;
    }

    private void Refresh()
    {
        if (inventoryManager == null || swordObject == null) return;

        bool hasWeapon = inventoryManager.GetEquippedWeapon() != null;

        swordObject.SetActive(hasWeapon);

        // ✅ 장착해서 무기 켜는 순간, 자동재생되는 vfxgraph만 OFF
        if (hasWeapon)
            DisableAutoPlayVFXGraphsOnly(swordObject.transform);
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
                    if (vfx != null) vfx.Stop(); // 혹시 켜지며 재생되면 멈춤

                    // ✅ 이 오브젝트만 꺼서 장착 순간 보이는 현상 차단
                    t.gameObject.SetActive(false);
                }
            }
        }
    }
}
