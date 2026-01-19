using UnityEngine;
using UnityEngine.UI;

public class MapIconUI : MonoBehaviour
{
    [Header("UI 컴포넌트 연결")]
    [SerializeField] private Image iconImage;
    [SerializeField] private Button btn;

    [Header("텔레포트 설정")]
    [SerializeField] private Transform teleportTarget;

    [Header("전투 제한 (몬스터 감지)")]
    [SerializeField] private float checkRadius = 20.0f;
    [SerializeField] private LayerMask monsterLayer;

    private bool isUnlocked = false;

    private void Start()
    {
        if (!isUnlocked)
        {
            this.gameObject.SetActive(false);
        }
        else
        {
            UpdateVisualState();
        }
    }

    public void UnlockIcon()
    {
        if (isUnlocked) return;
        isUnlocked = true;
        this.gameObject.SetActive(true);
        UpdateVisualState();
    }
    private void UpdateVisualState()
    {
        if (iconImage != null)
        {
            iconImage.color = Color.white;
        }

        if (btn != null)
        {
            btn.interactable = true;
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(TeleportPlayer);
        }
    }

    public void TeleportPlayer()
    {
        if (!isUnlocked) return;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            Debug.LogError("Player 태그를 가진 오브젝트를 찾을 수 없습니다!");
            return;
        }
        Collider[] nearbyEnemies = Physics.OverlapSphere(player.transform.position, checkRadius, monsterLayer);

        if (nearbyEnemies.Length > 0)
        {
            Debug.Log($"[이동 실패] 주변에 적이 {nearbyEnemies.Length}마리 있습니다! (전투 중 이동 불가)");
            return;
        }

        CharacterController cc = player.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;

        UnityEngine.AI.NavMeshAgent agent = player.GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (agent != null)
        {
            agent.Warp(teleportTarget.position);
        }
        else
        {
            player.transform.position = teleportTarget.position;
        }

        Rigidbody rb = player.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.position = teleportTarget.position;
        }
        if (cc != null) cc.enabled = true;

        Debug.Log("축복으로 이동했습니다.");

        MapController mapController = FindObjectOfType<MapController>();
        if (mapController != null)
        {
            mapController.SendMessage("ToggleMap", SendMessageOptions.DontRequireReceiver);
        }
    }
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1, 0, 0, 0.3f);
        Gizmos.DrawWireSphere(transform.position, checkRadius);
    }
}