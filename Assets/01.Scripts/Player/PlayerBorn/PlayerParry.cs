using UnityEngine;

public class PlayerParry : MonoBehaviour
{
    [Header("상태(Combat이 제어)")]
    public bool isParryStance = false;

    [Header("참조")]
    [SerializeField] private PlayerCombat combat;
    [SerializeField] private WeaponHitBox myHitBox; // MonsterParryHandler.TryParry에 넘길 hitBox(없으면 null도 가능)
    [SerializeField] private Transform origin;      // 탐지 기준점(가슴/무기/카메라 앞)

    [Header("탐지")]
    [SerializeField] private LayerMask parryableMask; // 몬스터 레이어
    [SerializeField] private float range = 2.0f;
    [SerializeField] private float angle = 120f;
    [SerializeField] private int maxHits = 16;

    private Collider[] hits;
    private bool consumedThisStance;

    private bool tryWindowOpen;

    private Transform lastAttacker;
    private Vector3 lastHitPoint;

    public Transform GetLastAttacker() => lastAttacker;
    public Vector3 GetLastHitPoint() => lastHitPoint;

    private void Awake()
    {
        if (!combat) combat = GetComponent<PlayerCombat>();
        if (!origin) origin = transform;
        if (!myHitBox) myHitBox = GetComponentInChildren<WeaponHitBox>();
        hits = new Collider[Mathf.Max(1, maxHits)];
    }

    private void Update()
    {
        if (!isParryStance) return;
        if (!tryWindowOpen) return;
        if (consumedThisStance) return;
        if (combat == null) return;

        if (!combat.WeaponEquipped) return;
        if (combat.IsAttacking) return;
        if (combat.IsDashing) return;

        var target = FindBestParryable(out Vector3 hitPoint, out Transform attackerTr);
        if (target == null) return;

        bool success = target.TryParry(myHitBox, hitPoint);
        if (!success) return;

        consumedThisStance = true;
        lastAttacker = attackerTr;
        lastHitPoint = hitPoint;

        combat.OnParrySuccess(lastAttacker, lastHitPoint);
    }

    public void Anim_ParryTryWindowOn()
    {
        tryWindowOpen = true;
        consumedThisStance = false; // 윈도우 시작마다 리셋
    }

    public void Anim_ParryTryWindowOff()
    {
        tryWindowOpen = false;
    }

    public void EnterStance()
    {
        isParryStance = true;
        consumedThisStance = false;
        lastAttacker = null;
        lastHitPoint = Vector3.zero;
    }

    public void ExitStance()
    {
        isParryStance = false;
        consumedThisStance = false;
    }
    //나중에 삭제예정
    public void Anim_TryParryNow()
    {
        if (!isParryStance) return;
        if (combat == null) return;

        if (!combat.WeaponEquipped) return;
        if (combat.IsAttacking) return;
        if (combat.IsDashing) return;

        var target = FindBestParryable(out Vector3 hitPoint, out Transform attackerTr);
        if (target == null) return;

        consumedThisStance = true;

        bool success = target.TryParry(myHitBox, hitPoint);
        if (!success) return;

        lastAttacker = attackerTr;
        lastHitPoint = hitPoint;

        combat.OnParrySuccess(lastAttacker, lastHitPoint);
    }

    private IParryReceiver FindBestParryable(out Vector3 bestPoint, out Transform bestTransform)
    {
        bestPoint = origin.position;
        bestTransform = null;

        int count = Physics.OverlapSphereNonAlloc(origin.position, range, hits, parryableMask, QueryTriggerInteraction.Collide);
        Debug.Log($"[PLY] Overlap count = {count}", this);
        if (count <= 0) return null;

        IParryReceiver best = null;
        float bestScore = float.NegativeInfinity;

        Vector3 fwd = transform.forward;
        fwd.y = 0f;
        if (fwd.sqrMagnitude < 0.0001f) fwd = Vector3.forward;
        fwd.Normalize();

        float half = angle * 0.5f;

        for (int i = 0; i < count; i++)
        {
            var col = hits[i];
            if (!col) continue;

            var receiver = col.GetComponentInParent<IParryReceiver>();
            if (receiver == null) continue;
            if (receiver is Component compp)
            {
                Debug.Log($"[PLY] Candidate: {compp.name} (col={col.name})", this);
            }

            Vector3 p = col.ClosestPoint(origin.position);

            Vector3 to = p - transform.position;
            to.y = 0f;
            float dist = Mathf.Max(0.0001f, to.magnitude);

            float a = Vector3.Angle(fwd, to / dist);
            if (a > half) continue;

            float score = -(a * 2f) - dist;
            if (score > bestScore)
            {
                bestScore = score;
                best = receiver;
                bestPoint = p;

                // ✅ 여기만 안전하게 바꿔
                if (receiver is Component comp)
                {
                    bestTransform = comp.transform;
                    Debug.Log($"[PLY] BEST -> {bestTransform.name}  dist={dist:F2}  angle={a:F1}  score={score:F2}", this);
                }
            }
        }
        Debug.Log($"[PLY] Final BEST = {(bestTransform ? bestTransform.name : "null")}", this);
        return best;
    }

}
