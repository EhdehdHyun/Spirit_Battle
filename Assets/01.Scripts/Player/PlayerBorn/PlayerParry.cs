using UnityEngine;

public enum ParryTelegraphType { SlowYellow, FastRed }

public class PlayerParry : MonoBehaviour
{
    [Header("State")]
    public bool isParryStance;     // 패링 자세 중인지
    public bool windowOpen;
    public float windowEndTime;
    private float _successUntil;

    [Header("Facing")]
    [Range(-1f, 1f)] public float minFacingDot = 0.3f; // 정면 판정

    [Header("UI")]
    [SerializeField] private ParryTelegraphUI ui;

    private Transform _attacker;
    private float _stunTime;

    public bool IsWindowOpen => windowOpen && Time.time <= windowEndTime;
    public bool HasParrySuccess => Time.time <= _successUntil;

    private void Awake()
    {
        if (!ui) ui = GetComponentInChildren<ParryTelegraphUI>(true);
    }

    // 보스/몬스터가 "곧 공격 들어간다"면서 열어주는 창
    public void OpenWindow(Transform attacker, float duration, ParryTelegraphType type, float stunTime)
    {
        if (duration <= 0f) return;

        _attacker = attacker;
        _stunTime = stunTime;

        windowOpen = true;
        windowEndTime = Time.time + duration;

        ui?.Show(type, duration);
        // Debug.Log($"[Parry] Window OPEN type={type} dur={duration}");
    }

    public void CloseWindow()
    {
        windowOpen = false;
        _attacker = null;
        ui?.Hide();
        // Debug.Log("[Parry] Window CLOSE");
    }

    // 입력 시도 (패링 버튼 눌렀을 때)
    public bool TryConsumeInput()
    {
        if (!IsWindowOpen) return false;

        // 패링 자세가 아니라도 "타이밍만"으로 성공시키고 싶으면 이 줄 삭제
        if (!isParryStance) return false;

        // 정면 판정
        if (_attacker != null)
        {
            Vector3 to = _attacker.position - transform.position;
            to.y = 0f;
            Vector3 fwd = transform.forward; fwd.y = 0f;

            if (to.sqrMagnitude > 0.0001f)
            {
                to.Normalize(); fwd.Normalize();
                float dot = Vector3.Dot(fwd, to);
                if (dot < minFacingDot) return false;
            }
        }

        // 성공: 공격 프레임이 약간 뒤여도 인정하도록 버퍼
        _successUntil = Time.time + 0.25f;

        CloseWindow();
        return true;
    }

    public float GetStunTime() => _stunTime;
    public Transform GetAttacker() => _attacker;
}
