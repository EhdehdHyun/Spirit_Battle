using UnityEngine;

/// <summary>
/// 젤다 / 소울류 스타일 3인칭 액션 카메라
/// - 플레이어를 기준으로 회전하는 오비트 카메라
/// - 마우스로 자유 시점 회전
/// - 스크롤로 줌
/// - 벽 충돌 시 카메라를 앞으로 당김
/// </summary>
public class ThirdPersonActionCamera : MonoBehaviour
{
    [Header("타겟 설정")]
    public Transform target;           // 플레이어 Transform
    public float pivotHeight = 1.6f;   // 타겟 기준 피벗 높이(대략 머리/목 정도)

    [Header("거리")]
    public float distance = 4.5f;      // 기본 거리
    public float minDistance = 2f;     // 최소 줌
    public float maxDistance = 6f;     // 최대 줌

    [Header("회전 설정")]
    public float mouseSensitivityX = 200f;  // 좌우 회전 속도
    public float mouseSensitivityY = 140f;  // 상하 회전 속도
    public float minPitch = -30f;          // 카메라가 너무 위로 안 가게
    public float maxPitch = 70f;           // 너무 아래로 안 가게

    [Header("부드러움")]
    public float followLerp = 20f;    // 위치/회전 보간 속도
    public float zoomSpeed = 5f;      // 줌 속도

    [Header("카메라 충돌")]
    public LayerMask collisionMask;   // 벽/지형 레이어
    public float collisionRadius = 0.2f;
    public float collisionOffset = 0.1f; // 벽에서 살짝 띄우기

    float _yaw;
    float _pitch;
    float _currentDistance;

    void Start()
    {
        if (target == null)
        {
            Debug.LogError("ThirdPersonActionCamera: target이 비어있음.");
            enabled = false;
            return;
        }

        _currentDistance = distance;

        // 초기 yaw/pitch 설정
        Vector3 toCam = (transform.position - GetPivotPosition()).normalized;
        _yaw = Mathf.Atan2(toCam.x, toCam.z) * Mathf.Rad2Deg;
        _pitch = Mathf.Asin(toCam.y) * Mathf.Rad2Deg;

        // 마우스 커서 잠그고 숨기기 (원하면 끄면 됨)
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void LateUpdate()
    {
        if (target == null) return;

        HandleRotationInput();
        HandleZoomInput();
        UpdateCameraPosition();
    }

    Vector3 GetPivotPosition()
    {
        return target.position + Vector3.up * pivotHeight;
    }

    void HandleRotationInput()
    {
        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");

        _yaw += mouseX * mouseSensitivityX * Time.deltaTime;
        _pitch -= mouseY * mouseSensitivityY * Time.deltaTime;
        _pitch = Mathf.Clamp(_pitch, minPitch, maxPitch);
    }

    void HandleZoomInput()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.001f)
        {
            distance -= scroll * zoomSpeed;
            distance = Mathf.Clamp(distance, minDistance, maxDistance);
        }
    }

    void UpdateCameraPosition()
    {
        Vector3 pivot = GetPivotPosition();

        // yaw, pitch로 회전 만들기
        Quaternion rot = Quaternion.Euler(_pitch, _yaw, 0f);

        // 회전에 따른 카메라 로컬 오프셋 
        Vector3 localOffset = new Vector3(0f, 0f, -distance);

        // 원하는 카메라 위치
        Vector3 desiredPos = pivot + rot * localOffset;

        // 충돌 검사 (pivot -> desiredPos)
        Vector3 dir = (desiredPos - pivot).normalized;
        float targetDist = localOffset.magnitude;

        if (Physics.SphereCast(pivot, collisionRadius, dir, out RaycastHit hit, targetDist, collisionMask, QueryTriggerInteraction.Ignore))
        {
            float adjustedDist = hit.distance - collisionOffset;
            adjustedDist = Mathf.Max(adjustedDist, minDistance * 0.3f); // 완전 0까지 붙지 않게
            Vector3 adjustedOffset = dir * adjustedDist;
            desiredPos = pivot + adjustedOffset;
        }

        // 부드럽게 이동/회전
        transform.position = Vector3.Lerp(transform.position, desiredPos, followLerp * Time.deltaTime);
        transform.rotation = Quaternion.Slerp(transform.rotation, rot, followLerp * Time.deltaTime);
    }
}

