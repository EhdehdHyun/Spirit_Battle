using UnityEngine;

public class PlayerCharacter : CharacterBase
{
    [Header("캐싱")]
    [SerializeField] private PlayerInputController input;
    [SerializeField] private PhysicsCharacter physicsChar;
    [SerializeField] private PlayerAnimation anim;
    [SerializeField] private PlayerCombat combat;
    [SerializeField] private PlayerParry parry;


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


    protected override void OnDie(DamageInfo info)
    {
        // 입력 / 이동 락, 죽음 애니메이션
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

    public void RespawnAt(Vector3 position)
    {
        // 위치 이동
        transform.position = position;

        // HP 풀피
        RestoreFullHp(notify: true);

        // 다시 조작 가능
        input?.Unlock();
        if (physicsChar != null) physicsChar.SetMovementLocked(false);

    }
}
