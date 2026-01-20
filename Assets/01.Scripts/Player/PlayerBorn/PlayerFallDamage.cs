using UnityEngine;

[RequireComponent(typeof(PhysicsCharacter))]
public class PlayerFallDamage : MonoBehaviour
{
    [Header("Fall Damage")]
    [SerializeField] bool enable = true;
    [SerializeField] float safeDropHeight = 3f;
    [SerializeField] float damagePerMeter = 10f;
    [SerializeField] float lethalDropHeight = 25f;
    [SerializeField] float minDamage = 1f;
    [SerializeField] float maxDamage = 9999f;

    [Header("Ground Probe")]
    [SerializeField] LayerMask groundMask;
    [SerializeField] float probeUpOffset = 0.2f;
    [SerializeField] float probeRadius = 0.25f;
    [SerializeField] float probeMaxDistance = 3f;

    [Header("Anti False Positive")]
    [SerializeField] float minAirTime = 0.12f;
    [SerializeField] float minFallSpeed = 6f;
    [SerializeField] bool ignoreIfDashedOnTakeoff = true;
    [SerializeField] bool debugLog = false;

    PhysicsCharacter pc;
    Rigidbody rb;
    CharacterBase ch;

    bool wasGrounded;
    bool tracking;
    bool dashedOnTakeoff;

    float takeoffGroundY;
    float airTime;
    float minYVel; // 가장 낮은 y속도 =최대 낙하속도

    void Awake()
    {
        pc = GetComponent<PhysicsCharacter>();
        rb = GetComponent<Rigidbody>();
        ch = GetComponentInParent<CharacterBase>();

        wasGrounded = pc != null && pc.IsGrounded;
        ResetTracking();
    }

    public void ResetTracking()
    {
        tracking = false;
        airTime = 0f;
        minYVel = 0f;
        dashedOnTakeoff = false;
        wasGrounded = pc != null && pc.IsGrounded;
        takeoffGroundY = SampleGroundY(transform.position.y);
    }

    void FixedUpdate()
    {
        if (!enable || pc == null) return;

        bool grounded = pc.IsGrounded;

        // 이륙 시작
        if (!tracking && wasGrounded && !grounded)
        {
            tracking = true;
            airTime = 0f;
            minYVel = 0f;

            dashedOnTakeoff = pc.IsDashing;
            takeoffGroundY = SampleGroundY(transform.position.y);
        }

        // 공중 동안 낙하 속도 기록
        if (tracking)
        {
            airTime += Time.fixedDeltaTime;
            if (rb != null) minYVel = Mathf.Min(minYVel, rb.velocity.y);
        }

        // 착지
        if (tracking && !wasGrounded && grounded)
        {
            tracking = false;

            if (ignoreIfDashedOnTakeoff && dashedOnTakeoff)
            {
                if (debugLog) Debug.Log("[FallDamage] ignored (dashed on takeoff)");
                wasGrounded = grounded;
                return;
            }

            float landingGroundY = SampleGroundY(transform.position.y);
            float drop = takeoffGroundY - landingGroundY;
            float fallSpeed = -minYVel;

            if (debugLog)
                Debug.Log($"[FallDamage] drop={drop:F2} air={airTime:F2} fallSpeed={fallSpeed:F2}");

            if (airTime >= minAirTime && fallSpeed >= minFallSpeed && drop > safeDropHeight)
            {
                float dmg = (drop >= lethalDropHeight)
                    ? maxDamage
                    : Mathf.Clamp((drop - safeDropHeight) * damagePerMeter, minDamage, maxDamage);

                if (ch != null)
                {
                    ch.TakeDamage(new DamageInfo { amount = dmg });
                }
            }
        }

        wasGrounded = grounded;
    }

    float SampleGroundY(float fallbackY)
    {
        Vector3 origin = transform.position + Vector3.up * probeUpOffset;

        RaycastHit hit;
        if (Physics.SphereCast(origin, probeRadius, Vector3.down, out hit, probeMaxDistance, groundMask, QueryTriggerInteraction.Ignore))
            return hit.point.y;

        return fallbackY; // 못 맞추면 현재 높이로 이상한 “아래 바닥” 안 잡게
    }
}
