using UnityEngine;
using TMPro;

public class PlayerInteraction : MonoBehaviour
{
    [Header("상호작용 탐지")]
    public float interactRadius = 2f;
    public LayerMask interactableLayer;

    [Header("UI")]
    public TextMeshProUGUI interactText;

    private IInteractable currentTarget;

    private void Update()
    {
        FindInteractable();

        // 🔵 옛날 Input 시스템으로 F키 감지
        if (Input.GetKeyDown(KeyCode.F))
        {
            Debug.Log("[PlayerInteraction] F 키 입력 감지 (Old Input)!");

            if (currentTarget != null)
            {
                var mb = currentTarget as MonoBehaviour;
                string name = mb != null ? mb.gameObject.name : "알 수 없는 오브젝트";
                Debug.Log($"[PlayerInteraction] {name} 에 상호작용 시도");
                currentTarget.Interact();
            }
            else
            {
                Debug.Log("[PlayerInteraction] F 눌렀지만 currentTarget이 없음");
            }
        }
    }

    private void FindInteractable()
    {
        Collider[] hits = Physics.OverlapSphere(
            transform.position,
            interactRadius,
            interactableLayer
        );

        if (hits.Length == 0)
        {
            currentTarget = null;
            if (interactText != null)
                interactText.gameObject.SetActive(false);
            return;
        }

        Collider closest = hits[0];
        float closestDist = Vector3.Distance(transform.position, closest.transform.position);

        for (int i = 1; i < hits.Length; i++)
        {
            float dist = Vector3.Distance(transform.position, hits[i].transform.position);
            if (dist < closestDist)
            {
                closest = hits[i];
                closestDist = dist;
            }
        }

        currentTarget = closest.GetComponent<IInteractable>();

        if (currentTarget != null && interactText != null)
        {
            interactText.text = currentTarget.GetInteractPrompt();
            interactText.gameObject.SetActive(true);
        }
        else if (interactText != null)
        {
            interactText.gameObject.SetActive(false);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactRadius);
    }
}
