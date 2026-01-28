using UnityEngine;

public class PlayerCharacter : CharacterBase
{
    [Header("캐싱")]
    [SerializeField] private PlayerInputController input;
    [SerializeField] private PhysicsCharacter physicsChar;
    [SerializeField] private PlayerAnimation anim;
    [SerializeField] private PlayerCombat combat;
    [SerializeField] private PlayerParry parry;

    [Header("튜토보스 전용 사망 대사")]
    [SerializeField]
    private string[] tutorialBossDeathLines =
    {
        "당신 혼자로는 아직 무리입니다…!",
        "이 힘을 사용하세요!"
    };

    protected override void Awake()
    {
        base.Awake();
        input = GetComponent<PlayerInputController>();
        physicsChar = GetComponent<PhysicsCharacter>();
        anim = GetComponent<PlayerAnimation>();
        combat = GetComponent<PlayerCombat>();
        parry = GetComponent<PlayerParry>();
    }

    protected override float GetIncomingDamageMultiplier(DamageInfo info)
    {
        if (parry != null && parry.IsParryGuardActive)
            return 0f;
        return 1f;
    }

    public override void OnDie(DamageInfo info)
    {
        base.OnDie(info);

        // 죽음 전이로 EvParryEnd 못 타도 상태가 남지 않게 강제 정리
        parry?.ForceCancelParry();

        //게임오버 됐을 때 게임오버 화면 제외 모든 UI 숨기는 기능
        UIVisibilityManager.Instance?.HideAllExceptGameOver();

        GlobalInputBlocker.SetKeyboardBlocked(false);

        //탭, M키 입력 차단
        GlobalInputBlocker.SetKeyBlocked(KeyCode.Tab, true);
        GlobalInputBlocker.SetKeyBlocked(KeyCode.M, true);

        // 입력 / 이동 락, 죽음 애니메이션
        input?.Lock();
        physicsChar.SetMovementLocked(true);
        anim?.PlayDie();

        var ui = GameOverUI.Instance;
        if (ui == null)
        {
            Debug.LogWarning("[PlayerCharacter] GameOverUI.Instance가 없음");
            return;
        }

        // “튜토보스 3페이즈 강제 이벤트”로 죽은 경우만 대사 모드
        if (info.reason == DamageReason.TutorialBossPhase3Finale)
            ui.ShowTutorialDeath("YOU DIED", tutorialBossDeathLines);
        else
            ui.ShowDeath("YOU DIED");
    }

    protected override void OnDamaged(DamageInfo info)
    {
        if (LastFinalDamage < 0f) return;
        if (physicsChar.IsDashing) return;

        HitVignetteFx.Instance?.Play(1f);

        if (LastHeavyHit)
            anim.PlayHitMotion();
    }

    public void RespawnAt(Vector3 position)
    {
        // 위치 이동
        transform.position = position;

        // 물리 리셋
        var rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // HP 풀피
        RestoreFullHp(notify: true);

        // 다시 조작 가능
        input?.Unlock();
        if (physicsChar != null) physicsChar.SetMovementLocked(false);
        GlobalInputBlocker.SetKeyBlocked(KeyCode.Tab, false);
        GlobalInputBlocker.SetKeyBlocked(KeyCode.M, false);

    }
    public void Rest()
    {
        RestoreFullHp(notify: true);

        var rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        Debug.Log("플레이어 휴식: 체력이 모두 회복되었습니다.");
    }
}
