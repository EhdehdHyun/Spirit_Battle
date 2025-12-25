using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//플레이어 허브로 사용
public class PlayerCharacter : CharacterBase
{
    [Header("캐싱")]
    [SerializeField] private PlayerInputController input;
    [SerializeField] private PhysicsCharacter physicsChar;
    [SerializeField] private PlayerAnimation anim;
    [SerializeField] private PlayerCombat combat;
    [SerializeField] private PlayerParry parry;
    [SerializeField] private WeaponHitBox weaponHitBox;

    [Header("Death UI")]
    [Tooltip("죽는 애니메이션이 조금 보인 뒤에 UI 띄우고 싶으면 값 올리기")]
    [SerializeField] private float gameOverShowDelay = 1.2f;

    private bool deathHandled = false;

    // 튜토리얼 전용 사망 연출 예약(보스가 미리 세팅해줄 수 있음)
    private bool pendingTutorialDeath = false;
    private string[] pendingLines;

    protected override void Awake()
    {
        base.Awake();
        input = GetComponent<PlayerInputController>();
        physicsChar = GetComponent<PhysicsCharacter>();
        anim = GetComponent<PlayerAnimation>();
        combat = GetComponent<PlayerCombat>();
        parry = GetComponent<PlayerParry>();
        weaponHitBox = GetComponentInChildren<WeaponHitBox>();
    }

    public void PrepareTutorialDeath(string[] lines)
    {
        pendingTutorialDeath = true;
        pendingLines = lines;
    }

    protected override float GetIncomingDamageMultiplier(DamageInfo info)
    {
        if (parry != null && parry.IsParryGuardActive)
            return 0f;

        return 1f;
    }

    protected override void OnDie(DamageInfo info)
    {
        // 여기서 입력 락, 애니, 게임오버 처리
        input?.Lock();
        physicsChar.SetMovementLocked(true);
        anim?.PlayDie(); // 함수명은 네꺼에 맞춰

        StartCoroutine(Co_ShowGameOver());
    }

    protected override void OnDamaged(DamageInfo info)
    {
        if (LastFinalDamage < 0f) return;

        if (physicsChar.IsDashing) return;
        if (LastHeavyHit) 
            anim.PlayHitMotion();
    }

    private IEnumerator Co_ShowGameOver()
    {
        if (gameOverShowDelay > 0f)
            yield return new WaitForSecondsRealtime(gameOverShowDelay);

        var ui = GameOverUI.Instance;
        if (ui == null) yield break;

        if (pendingTutorialDeath)
        {
            ui.ShowTutorialSequence(
                "YOU DIED",
                pendingLines ?? new[] { "아직 힘을 잃지마요." },
                onFinished: () =>
                {
                    // TODO: 여기서 스킬 해금/대사 시작/리스폰 버튼 띄우기 등 연결
                    // 예) SkillManager.Instance.Unlock(...);
                    // 예) DialogueManager.Instance.Start(...);
                }
            );
        }
        else
        {
            ui.ShowNormalDeath("YOU DIED");
        }
    }

    public void RespawnAt(Vector3 pos, Quaternion rot)
    {
        // 위치 이동
        transform.SetPositionAndRotation(pos, rot);

        // 물리 속도 초기화(튀는 것 방지)
        if (TryGetComponent<Rigidbody>(out var rb))
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // HP 풀로 회복되서 살아남
        currentHp = maxHp;

        // 입력/이동 잠금 해제
        input?.Unlock();
        if (physicsChar != null)
            physicsChar.SetMovementLocked(false);

        // (선택) 죽음 애니 상태가 남아있으면 초기화
        var animator = GetComponentInChildren<Animator>();
        if (animator != null)
        {
            animator.Rebind();
            animator.Update(0f);
        }

        // 무기 히트박스/전투 상태 초기화가 필요하면 여기서
    }
}
