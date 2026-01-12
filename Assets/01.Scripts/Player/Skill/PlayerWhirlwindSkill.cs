using System.Collections.Generic;
using UnityEngine;

//범위 공격을 위한 언터페이스
public interface IAoeDamageable
{
    void ApplyAoeDamage(float damage, Transform attacker);
}

public class PlayerWhirlwindSkill : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerAnimation playerAnim;
    [SerializeField] private PhysicsCharacter physicsCharacter;
    [SerializeField] private PlayerInputController playerInput; // 있으면 잠금 체크용
    [SerializeField] private PlayerCombat combat; // 있으면 공격중 체크용
    [SerializeField] private PlayerParry parry; // 있으면 패링중 체크용

    [Header("Input")]
    [Tooltip("스킬 사용 키")]
    public KeyCode debugKey = KeyCode.None;

    [Header("Skill")]
    [SerializeField] private float cooldown = 5f;
    [SerializeField] private float damage = 25f;

    [Header("AOE")]
    [SerializeField] private LayerMask targetMask;
    [SerializeField] private float radius = 3.0f;
    [SerializeField] private Vector3 centerOffset = new Vector3(0f, 1.0f, 0f);
    [SerializeField] private int maxHits = 32;

    [Header("VFX")]
    [SerializeField] private GameObject vfxPrefab;
    [SerializeField] private Transform vfxSpawn;
    [SerializeField] private GameObject vfxObject;
    private GameObject _spawnedVfx;
    [SerializeField] private float vfxAutoOffTime = 1.0f;

    [Header("Movement Lock")]
    [SerializeField] private bool lockMovementDuringCast = true;

    [SerializeField] private Animator animator;
    [SerializeField] private string whirlwindStateName = "Whirlwind";

    private float _nextUseTime;
    private bool _isCasting;
    private float _vfxOffAt;

    private Collider[] _overlap;
    private readonly HashSet<int> _hitRootIds = new HashSet<int>();

    void Awake()
    {
        if (playerAnim == null) playerAnim = GetComponentInChildren<PlayerAnimation>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (physicsCharacter == null) physicsCharacter = GetComponent<PhysicsCharacter>();
        if (playerInput == null) playerInput = GetComponent<PlayerInputController>();
        if (combat == null) combat = GetComponent<PlayerCombat>();
        if (parry == null) parry = GetComponent<PlayerParry>();

        _overlap = new Collider[Mathf.Max(8, maxHits)];
    }

    void Update()
    {

        if (debugKey != KeyCode.None && Input.GetKeyDown(debugKey))
        {
            OnSkillInput();
        }

        if (_vfxOffAt > 0f && Time.time >= _vfxOffAt)
        {
            SetVfxActive(false);
            _vfxOffAt = 0f;
        }
    }

    public void CancelByDash()
    {
        if (!_isCasting) return;
        ForceEndCast();
    }

    public void OnSkillInput()
    {
        if (!CanCastNow()) return;

        _nextUseTime = Time.time + cooldown;
        _isCasting = true;
        _hitRootIds.Clear();

        if (lockMovementDuringCast && physicsCharacter != null)
            physicsCharacter.SetMovementLocked(true);

        // 애니메이션 시작
        playerAnim?.PlayWhirlwind();
        
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

        if (playerInput != null && playerInput.isLocked) return false;
        if (physicsCharacter != null && physicsCharacter.IsDashing) return false;
        if (combat != null && combat.IsAttacking) return false;
        if (parry != null && parry.isParryStance) return false;

        if (combat != null && !combat.WeaponEquipped) return false;

        return true;
    }

    public void EvWhirlwind_Hit()
    {
        if (!_isCasting) return;

        SetVfxActive(true);
        _vfxOffAt = (vfxAutoOffTime > 0f) ? Time.time + vfxAutoOffTime : 0f;

        DoAoeDamage();
    }

    public void EvWhirlwind_End() => ForceEndCast();

    private void OnDisable()
    {
        ForceEndCast();
    }

    private void ForceEndCast()
    {
        if (!_isCasting) return;

        _isCasting = false;

        if (lockMovementDuringCast && physicsCharacter != null)
            physicsCharacter.SetMovementLocked(false);

        SetVfxActive(false);
        _vfxOffAt = 0f;
        _hitRootIds.Clear();
    }

    private void DoAoeDamage()
    {
        Vector3 center = transform.position + centerOffset;

        int count = Physics.OverlapSphereNonAlloc(
            center,
            radius,
            _overlap,
            targetMask,
            QueryTriggerInteraction.Ignore
        );

        for (int i = 0; i < count; i++)
        {
            Collider col = _overlap[i];
            if (col == null) continue;

            Transform root = col.attachedRigidbody != null
                ? col.attachedRigidbody.transform.root
                : col.transform.root;

            int id = root.GetInstanceID();
            if (_hitRootIds.Contains(id)) continue;
            _hitRootIds.Add(id);

            var dmg = root.GetComponentInChildren<IAoeDamageable>();
            if (dmg != null)
            {
                dmg.ApplyAoeDamage(damage, transform);
                continue;
            }

            root.gameObject.SendMessage("TakeDamage", damage, SendMessageOptions.DontRequireReceiver);
            root.gameObject.SendMessage("ApplyDamage", damage, SendMessageOptions.DontRequireReceiver);
        }

        Debug.Log($"[Whirlwind] Overlap count = {count}");
    }

    private void SetVfxActive(bool on)
    {
        if (vfxPrefab != null)
        {
            if (on)
            {
                if (_spawnedVfx != null) Destroy(_spawnedVfx);

                Transform spawn = vfxSpawn != null ? vfxSpawn : transform;
                _spawnedVfx = Instantiate(vfxPrefab, spawn.position, spawn.rotation, spawn);

                if (vfxAutoOffTime > 0f)
                    Destroy(_spawnedVfx, vfxAutoOffTime);
            }
            else
            {
                if (_spawnedVfx != null)
                {
                    Destroy(_spawnedVfx);
                    _spawnedVfx = null;
                }
            }
            return;
        }
        if (vfxObject != null)
            vfxObject.SetActive(on);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.matrix = Matrix4x4.identity;
        Gizmos.DrawWireSphere(transform.position + centerOffset, radius);
    }
}
