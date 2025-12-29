using UnityEngine;

public class LoadingSpinnerRotator : MonoBehaviour
{
    [SerializeField] private float degreesPerSecond = 360f;
    [Tooltip("Time.timeScale=0 이어도 돌아가게(권장)")]
    [SerializeField] private bool useUnscaledTime = true;

    private RectTransform rt;

    private void Awake()
    {
        rt = transform as RectTransform;
    }

    private void OnEnable()
    {
        // 켤 때 각도 초기화(원하면)
        if (rt != null) rt.localRotation = Quaternion.identity;
    }

    private void Update()
    {
        float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        float delta = degreesPerSecond * dt;

        // UI는 보통 Z축 회전
        transform.Rotate(0f, 0f, -delta);
    }
}
