using UnityEngine;
using UnityEngine.UI;

public class MapIconUI : MonoBehaviour
{
    [Header("설정")]
    public Transform teleportTarget; // 이동할 실제 월드 위치 (축복 오브젝트)
    public Image iconImage;          // 아이콘 이미지 컴포넌트
    public Button btn;               // 버튼 컴포넌트

    private bool isUnlocked = false; // 현재 활성화 상태

    private void Start()
    {
        LockIcon();

        btn.onClick.AddListener(TeleportPlayer);
    }

    // 아이콘 잠금 (회색, 클릭 불가)
    public void LockIcon()
    {
        isUnlocked = false;
        iconImage.color = Color.gray;
        btn.interactable = false;
    }

    public void UnlockIcon()
    {
        if (isUnlocked) return;

        isUnlocked = true;
        iconImage.color = Color.white;
        btn.interactable = true;

        Debug.Log("지도 아이콘이 활성화되었습니다!");
    }

    public void TeleportPlayer()
    {
        if (!isUnlocked) return;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
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
            if (cc != null) cc.enabled = true;

            Rigidbody rb = player.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.position = teleportTarget.position;
            }

            MapController mapController = FindObjectOfType<MapController>();
            if (mapController != null)
            {
            }

            Debug.Log("순간이동 완료!");
        }
        else
        {
            Debug.LogError("Player 태그를 찾을 수 없습니다!");
        }
    }
}