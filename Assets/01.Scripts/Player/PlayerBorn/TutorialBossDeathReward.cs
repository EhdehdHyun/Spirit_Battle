using System.Collections;
using UnityEngine;

public class TutorialBossDeathReward : MonoBehaviour
{
    [Header("Auto Find (비워도 됨)")]
    [SerializeField] private CharacterBase playerCharacter;
    [SerializeField] private PlayerAbility playerAbility;

    [Header("Tutorial GameOver Text")]
    [SerializeField] private string title = "Game Over";
    [TextArea]
    [SerializeField]
    private string[] lines =
    {
        "괜찮아. 여기서 배우면 돼!",
        "죽어도 스킬이 하나씩 열릴 거야.",
        "다시 한 번 해보자."
    };

    [Header("Reward")]
    [SerializeField] private bool unlockSkill1OnDeath = true;

    private bool wasAlive = true;
    private bool handledThisDeath = false;

    private void Awake()
    {
        if (playerCharacter == null)
            playerCharacter = GetComponentInParent<CharacterBase>() ?? GetComponent<CharacterBase>();

        if (playerAbility == null)
            playerAbility = GetComponentInParent<PlayerAbility>() ?? GetComponent<PlayerAbility>();
    }

    private void Update()
    {
        if (playerCharacter == null) return;

        bool aliveNow = playerCharacter.IsAlive;

        // 살아있다가 -> 죽는 순간 1회
        if (wasAlive && !aliveNow && !handledThisDeath)
        {
            handledThisDeath = true;
            OnPlayerDied();
        }

        // 부활해서 다시 살아나면 다음 죽음 처리 가능하게 리셋
        if (!wasAlive && aliveNow)
        {
            handledThisDeath = false;
        }

        wasAlive = aliveNow;
    }

    private void OnPlayerDied()
    {
        // 튜토보스전일 때만 발동
        if (TutorialBossMarker.ActiveCount <= 0)
            return;

        // 스킬 해금
        if (unlockSkill1OnDeath && playerAbility != null)
            playerAbility.Unlock(AbilityId.Skill1);

        // UI는 "다음 프레임"에 튜토로 확정 
        StartCoroutine(ShowTutorialGameOverNextFrame());
    }

    private IEnumerator ShowTutorialGameOverNextFrame()
    {
        // 기존 죽음 처리(ShowDeath)가 같은 프레임에 들어오면 먼저 실행되게 한 프레임 양보
        yield return null;

        // GameOverUI가 늦게 생길 수도 있으니 조금 기다렸다가 호출)
        float timeout = 1f;
        while (GameOverUI.Instance == null && timeout > 0f)
        {
            timeout -= Time.unscaledDeltaTime;
            yield return null;
        }

        if (GameOverUI.Instance != null)
            GameOverUI.Instance.ShowTutorialDeath(title, lines);
    }
}
