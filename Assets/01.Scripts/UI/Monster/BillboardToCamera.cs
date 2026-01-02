using UnityEngine;

public class BillboardToCamera : MonoBehaviour
{
    [SerializeField] private Camera targetCamera;
    [Tooltip("위 아래 기울임까지 따라갈지")]
    [SerializeField] private bool fullRotation = false;

    private void Awake()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;
    }
    private void LateUpdate()
    {
        if (targetCamera == null) return;

        Vector3 dir = transform.position - targetCamera.transform.position;

        if (!fullRotation)
        {
            dir.y = 0f;
        }

        if (dir.sqrMagnitude < 0.001f) return;

        transform.rotation = Quaternion.LookRotation(dir);
    }
}
