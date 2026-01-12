using UnityEngine;

public class PlayerTornadoSkill : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerAnimation playerAnim;
    [SerializeField] private PhysicsCharacter physicsCharacter;
    [SerializeField] private PlayerInputController playerInput;
    [SerializeField] private PlayerCombat combat;
    [SerializeField] private PlayerParry parry;

    [Header("Animator / Events")]
    [Tooltip("애니메이션 이벤트(Fire)가 호출될 때 생성됨")]
    [SerializeField] private TornadoProjectile projectilePrefab;
    [SerializeField] private Transform spawnPoint;

    [Header("Skill")]
    [SerializeField] private float cooldown = 8f;
    [SerializeField] private bool requireWeaponEquipped = true;
    [SerializeField] private bool lockMovementDuringCast = true;
    [SerializeField] private bool destroyProjectileOnCancelByDash = true;

    [Header("Projectile Settings")]
    [SerializeField] private LayerMask targetMask;
    [SerializeField] private float projectileSpeed = 10f;
    [SerializeField] private float maxDistance = 10f;
    [SerializeField] private float shrinkStartDistance = 2f;

    [Header("Pull/DOT Settings")]
    [SerializeField] private Vector3 centerOffset = new Vector3(0f, 1.0f, 0f);
    [SerializeField] private float pullRadius = 4f;
    [SerializeField] private float damageRadius = 3f;
    [SerializeField] private float pullMaxSpeed = 6f;
    [SerializeField] private float pullAcceleration = 40f;

    [SerializeField] private float damagePerTick = 10f;
    [SerializeField] private float tickInterval = 0.5f;

    [SerializeField] private int maxHits = 32;

    private float _nextUseTime;
    private bool _isCasting;
    private TornadoProjectile _spawned;

    private void Awake()
    {
        if (!playerAnim) playerAnim = GetComponentInChildren<PlayerAnimation>();
        if (!physicsCharacter) physicsCharacter = GetComponent<PhysicsCharacter>();
        if (!playerInput) playerInput = GetComponent<PlayerInputController>();
        if (!combat) combat = GetComponent<PlayerCombat>();
        if (!parry) parry = GetComponent<PlayerParry>();
        if (!spawnPoint) spawnPoint = transform;
    }

    public void OnSkillInput()
    {
        if (!CanCastNow()) return;

        _nextUseTime = Time.time + cooldown;
        _isCasting = true;

        if (lockMovementDuringCast && physicsCharacter)
            physicsCharacter.SetMovementLocked(true);

        playerAnim?.PlayTornado(); // 애니 트리거
        
        //스킬3회사용 튜토리얼 
        QuestManager.Instance.ReportProgress(
            CompleteCondition.UseSkill,
            0,   //TargetID
            1
        );
    }

    private bool CanCastNow()
    {
        if (Time.time < _nextUseTime) return false;
        if (_isCasting) return false;

        if (playerInput && playerInput.isLocked) return false;
        if (physicsCharacter && physicsCharacter.IsDashing) return false;
        if (combat && combat.IsAttacking) return false;
        if (parry && parry.isParryStance) return false;

        if (requireWeaponEquipped && combat && !combat.WeaponEquipped) return false;
        if (!projectilePrefab) return false;

        return true;
    }

    public void EvTornado_Fire()
    {
        if (!_isCasting) return;
        if (!projectilePrefab) return;

        if (_spawned != null) return;

        Transform sp = spawnPoint ? spawnPoint : transform;

        Vector3 forward = transform.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.0001f) forward = Vector3.forward;

        var go = Instantiate(projectilePrefab, sp.position, Quaternion.LookRotation(forward, Vector3.up));
        _spawned = go;

        go.Init(
            owner: transform,
            forward: forward,
            targets: targetMask,
            moveSpeed: projectileSpeed,
            travelDistance: maxDistance,
            shrinkStartDist: shrinkStartDistance,
            pullRadius: pullRadius,
            damageRadius: damageRadius,
            pullMaxSpd: pullMaxSpeed,
            pullAccel: pullAcceleration,
            dmgPerTick: damagePerTick,
            tickIntv: tickInterval,
            aoeCenterOffset: centerOffset,
            maxHitCount: maxHits
        );
    }

    public void EvTornado_End()
    {
        ForceEndCast();
    }

    public void CancelByDash()
    {
        if (!_isCasting) return;

        if (destroyProjectileOnCancelByDash && _spawned != null)
        {
            Destroy(_spawned.gameObject);
            _spawned = null;
        }

        ForceEndCast();
    }

    private void ForceEndCast()
    {
        if (!_isCasting) return;

        _isCasting = false;

        if (lockMovementDuringCast && physicsCharacter)
            physicsCharacter.SetMovementLocked(false);

        _spawned = null;
    }
}
