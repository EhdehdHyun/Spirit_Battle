using UnityEngine;

public class WorldArrowController : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private Transform target;
    [SerializeField] private Transform arrowMesh;

    [SerializeField] private float distance = 2.5f;
    [SerializeField] private float height = 1.8f;
    private void Awake()
    {
        gameObject.SetActive(false);
    }
    
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;

        if (target != null)
            gameObject.SetActive(true);
    }

    public void ClearTarget()
    {
        target = null;
        gameObject.SetActive(false);
    }
    void LateUpdate()
    {
        if (player == null || target == null) return;

        // 화살표 위치 (플레이어 기준)
        Vector3 dir = (target.position - player.position).normalized;
        Vector3 pos = player.position + dir * distance;
        pos.y += height;
        transform.position = pos;

        // 2방향 계산 (XZ 평면)
        Vector3 flatDir = target.position - transform.position;
        flatDir.y = 0f;

        if (flatDir.sqrMagnitude < 0.001f) return;

        //  각도 계산
        float angle = Mathf.Atan2(flatDir.z, flatDir.x) * Mathf.Rad2Deg;

        //  왼쪽이 기본인 스프라이트 보정
        arrowMesh.rotation = Quaternion.Euler(90f, -angle + 180f, 0f);
    }
}