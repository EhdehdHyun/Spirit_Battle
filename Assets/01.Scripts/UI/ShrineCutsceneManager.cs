using UnityEngine;
using System.Collections;

public class ShrineCutsceneManager : MonoBehaviour
{
    [Header("Cutscene Actor")]
    public GameObject cutscenePlayerPrefab;
    public Transform cutsceneSpawnPoint;

    [Header("Cutscene NPC")]
    public GameObject cutsceneNpcPrefab;
    public Transform npcSpawnPoint;
    public Vector3 npcOffset = new Vector3(1.2f, 0f, -0.3f);

    private GameObject cutscenePlayer;
    private GameObject cutsceneNpc;
    private Animator cutsceneAnimator;
    private Animator cutsceneNpcAnimator;

    [Header("UI")]
    public GameObject playerUIRoot;
    public GameObject cutsceneUIRoot;

    [Header("Letterbox UI")]
    public RectTransform topBar;
    public RectTransform bottomBar;
    public float letterboxHeight = 120f;
    public float letterboxDuration = 0.4f;

    [Header("Player & Camera")]
    public PlayerInputController realPlayer;
    public Camera playerCamera;
    public Camera cutsceneCamera;

    [Header("Movement")]
    public Transform walkEndPoint;
    public float walkSpeed = 1.5f;

    public void PlayCutscene()
    {
        StartCoroutine(Co_PlayCutscene());
    }

    IEnumerator Co_PlayCutscene()
    { 
        // 실제 플레이어 비활성화
        realPlayer.gameObject.SetActive(false);
        playerCamera.gameObject.SetActive(false);

        playerUIRoot.SetActive(false);
        cutsceneUIRoot.SetActive(true);
        cutsceneCamera.gameObject.SetActive(true);

        yield return StartCoroutine(Co_OpenLetterbox());

        // 컷씬 캐릭터 생성
        cutscenePlayer = Instantiate(
            cutscenePlayerPrefab,
            cutsceneSpawnPoint.position,
            cutsceneSpawnPoint.rotation
        );

        cutsceneNpc = Instantiate(
            cutsceneNpcPrefab,
            npcSpawnPoint.position,
            npcSpawnPoint.rotation
        );

        cutsceneAnimator = cutscenePlayer.GetComponent<Animator>();
        cutsceneNpcAnimator = cutsceneNpc.GetComponent<Animator>();

        cutsceneAnimator.Play("CS_WalkToShrine");
        cutsceneNpcAnimator.Play("CS_Walk");

        // 이동 루프
        while (Vector3.Distance(cutscenePlayer.transform.position, walkEndPoint.position) > 0.1f)
        {
            Vector3 moveDir =
                (walkEndPoint.position - cutscenePlayer.transform.position).normalized;

            cutscenePlayer.transform.position +=
                moveDir * walkSpeed * Time.deltaTime;

            cutsceneNpc.transform.position +=
                moveDir * walkSpeed * Time.deltaTime;

            cutscenePlayer.transform.rotation =
                Quaternion.Slerp(
                    cutscenePlayer.transform.rotation,
                    Quaternion.LookRotation(moveDir),
                    Time.deltaTime * 5f
                );

            cutsceneNpc.transform.rotation =
                Quaternion.Slerp(
                    cutsceneNpc.transform.rotation,
                    Quaternion.LookRotation(moveDir),
                    Time.deltaTime * 5f
                );

            yield return null;
        }
        yield return new WaitForSeconds(0.25f);
        OnCutsceneEnd();
    }

    void OnCutsceneEnd()
    {
        Vector3 finalPos = cutscenePlayer.transform.position;
        Quaternion finalRot = cutscenePlayer.transform.rotation;

        Destroy(cutscenePlayer);
        Destroy(cutsceneNpc);

        realPlayer.transform.position = finalPos;
        realPlayer.transform.rotation = finalRot;
        realPlayer.gameObject.SetActive(true);
        realPlayer.Unlock();

        StartCoroutine(Co_CloseLetterbox());

        cutsceneUIRoot.SetActive(false);
        playerUIRoot.SetActive(true);

        cutsceneCamera.gameObject.SetActive(false);
        playerCamera.gameObject.SetActive(true);
    }

    IEnumerator Co_OpenLetterbox()
    {
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / letterboxDuration;
            float h = Mathf.Lerp(0f, letterboxHeight, t);
            topBar.sizeDelta = new Vector2(0, h);
            bottomBar.sizeDelta = new Vector2(0, h);
            yield return null;
        }
    }

    IEnumerator Co_CloseLetterbox()
    {
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / letterboxDuration;
            float h = Mathf.Lerp(letterboxHeight, 0f, t);
            topBar.sizeDelta = new Vector2(0, h);
            bottomBar.sizeDelta = new Vector2(0, h);
            yield return null;
        }
    }
}
