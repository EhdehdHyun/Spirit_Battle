using UnityEngine;
using System.Collections;

enum CutsceneCameraMode
{
    StartCorner,
    FaceToFace,
    ShrineFocus
}

public class ShrineCutsceneManager : MonoBehaviour
{
    /* =======================
     * Cutscene Actors
     * ======================= */
    [Header("Cutscene Actor")]
    public GameObject cutscenePlayerPrefab;
    public Transform cutsceneSpawnPoint;

    [Header("Cutscene NPC")]
    public GameObject cutsceneNpcPrefab;
    public Transform npcSpawnPoint;

    private GameObject cutscenePlayer;
    private GameObject cutsceneNpc;
    private Animator cutsceneAnimator;
    private Animator cutsceneNpcAnimator;

    /* =======================
     * UI
     * ======================= */
    [Header("UI")]
    public GameObject playerUIRoot;
    public GameObject cutsceneUIRoot;

    [Header("Letterbox UI")]
    public RectTransform topBar;
    public RectTransform bottomBar;
    public float letterboxHeight = 120f;
    public float letterboxDuration = 0.4f;

    [Header("Dialogue")]
    public TMPro.TMP_Text dialogueText;

    /* =======================
     * Player & Camera
     * ======================= */
    [Header("Player & Camera")]
    public PlayerInputController realPlayer;
    public Camera playerCamera;
    public Camera cutsceneCamera;

    [Header("Camera Targets")]
    public Transform camStartCorner;
    public Transform camFaceToFace;
    public Transform camShrineFocus;
    
    [Header("FaceToFace Camera Follow")]
    public float faceToFaceFollowSpeed = 1.0f; // 플레이어 속도에 대한 비율
    
    [Header("Shrine Target")]
    public Transform shrineTarget;

    private CutsceneCameraMode cameraMode;
    
    private Vector3 prevPlayerPos;

    /* =======================
     * Movement
     * ======================= */
    [Header("Movement")]
    public Transform walkEndPoint;
    public float playerWalkSpeed = 1.0f;
    public float npcWalkSpeed = 0.6f;

    /* =======================
     * Cutscene State Flags
     * ======================= */
    private bool dialogueFinished;

    /* =======================
     * Public Entry
     * ======================= */
    public void PlayCutscene()
    {
        TutorialManager.Instance?.EndTutorialUI();
        StartCoroutine(Co_PlayCutscene());
    }

    /* =======================
     * Camera Control
     * ======================= */
    void LateUpdate()
    {
        if (!cutsceneCamera.gameObject.activeSelf || cutscenePlayer == null) return;

        switch (cameraMode)
        {
            case CutsceneCameraMode.StartCorner:
                FollowCorner();
                break;

            case CutsceneCameraMode.FaceToFace:
                FollowFaceToFace();
                break;

            case CutsceneCameraMode.ShrineFocus:
                FocusShrine();
                break;
        }
    }

    void FollowCorner()
    {
        cutsceneCamera.transform.position =
            Vector3.Lerp(
                cutsceneCamera.transform.position,
                camStartCorner.position,
                Time.deltaTime * 2f
            );

        cutsceneCamera.transform.LookAt(cutscenePlayer.transform.position + Vector3.up * 1.5f);
    }
    
    void FollowFaceToFace()
    {
        // 플레이어 이동량 계산
        Vector3 playerDelta = cutscenePlayer.transform.position - prevPlayerPos;

        // 플레이어가 이동한 만큼 FaceToFace 오브젝트를 -X로 이동
        camFaceToFace.position += Vector3.left * playerDelta.magnitude * faceToFaceFollowSpeed;

        prevPlayerPos = cutscenePlayer.transform.position;

        // 카메라는 FaceToFace 오브젝트만 따라감
        cutsceneCamera.transform.position =
            Vector3.Lerp(
                cutsceneCamera.transform.position,
                camFaceToFace.position,
                Time.deltaTime * 3f
            );

        // 항상 두 캐릭터 정면을 바라봄
        Vector3 mid =
            (cutscenePlayer.transform.position + cutsceneNpc.transform.position) * 0.5f;

        cutsceneCamera.transform.LookAt(mid + Vector3.up * 1.6f);
    }

    void FocusShrine()
    {
        Vector3 targetPos = new Vector3(
            camShrineFocus.position.x,
            Mathf.Max(camShrineFocus.position.y, 3.5f),
            camShrineFocus.position.z
        );

        cutsceneCamera.transform.position =
            Vector3.Lerp(
                cutsceneCamera.transform.position,
                targetPos,
                Time.deltaTime * 2f
            );

        //  rotation 직접 건드리지 말고 LookAt으로 통일
        cutsceneCamera.transform.LookAt(
            shrineTarget.position + Vector3.up * 2.0f
        );
    }

    /* =======================
     * Main Cutscene Flow
     * ======================= */
    IEnumerator Co_PlayCutscene()
    {
        dialogueFinished = false;

        // 실제 플레이어 비활성화
        realPlayer.gameObject.SetActive(false);
        playerCamera.gameObject.SetActive(false);

        playerUIRoot.SetActive(false);
        cutsceneUIRoot.SetActive(true);
        cutsceneCamera.gameObject.SetActive(true);

        // 카메라 초기 스냅
        cutsceneCamera.transform.position = camStartCorner.position;
        cutsceneCamera.transform.rotation = camStartCorner.rotation;

        yield return StartCoroutine(Co_OpenLetterbox());

        cameraMode = CutsceneCameraMode.StartCorner;

        // 대사 시작
        StartCoroutine(Co_PlayDialogue());

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
        //Face To Face()에 카메라가 따라가기 위한 값 저장
        prevPlayerPos = cutscenePlayer.transform.position;
        
        cutsceneAnimator = cutscenePlayer.GetComponent<Animator>();
        cutsceneNpcAnimator = cutsceneNpc.GetComponent<Animator>();

        cutsceneAnimator.Play("CS_WalkToShrine");
        cutsceneNpcAnimator.Play("CS_Walk");

        // 이동 루프
        while (Vector3.Distance(cutscenePlayer.transform.position, walkEndPoint.position) > 0.1f)
        {
            Vector3 moveDir =
                (walkEndPoint.position - cutscenePlayer.transform.position).normalized;

            cutscenePlayer.transform.position += moveDir * playerWalkSpeed * Time.deltaTime;
            cutsceneNpc.transform.position += moveDir * npcWalkSpeed * Time.deltaTime;

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
                    Time.deltaTime * 3f
                );

            yield return null;
        }
        //대사 끝날 때까지 대기
        while (!dialogueFinished)
        {
            yield return null;
        }

        OnCutsceneEnd();
    }

    /* =======================
     * Dialogue Timeline
     * ======================= */
    IEnumerator Co_PlayDialogue()
    {
        dialogueText.gameObject.SetActive(true);

        dialogueText.text = "난파가 되기전에 기억은 남아있나?";
        yield return new WaitForSeconds(2f);

        dialogueText.text = "기억이 희미하지만, 배를 탔었고 그 이후에 기억은 아직…";
        yield return new WaitForSeconds(2f);

        dialogueText.text = "천천히 생각하게나, 아직 정신이 온전히 않을테니..";
        yield return new WaitForSeconds(2f);

        dialogueText.text = "기억이 돌아올때까지 이 섬에서 잠쉬 쉬었다 가보는게 좋을것 같네";
        yield return new WaitForSeconds(2.5f);

        dialogueText.text = "…";
        yield return new WaitForSeconds(1.5f);

        cameraMode = CutsceneCameraMode.FaceToFace;
        dialogueText.text = "난 운명이라 생각한다네.. 자네에게 인간에게서는 찾아볼 수 없는 무엇인가 특별한 힘이 있다네..";
        yield return new WaitForSeconds(4f);

        dialogueText.text = "그것이 뭔지는 정확히 모르겠지만, 특별하다는것 확신할 수 있지.";
        yield return new WaitForSeconds(3f);

        dialogueText.text = "무리한 부탁일 수 있지만, 부디 이 섬을 도와줄 수 있겠나?";
        yield return new WaitForSeconds(3f);

        dialogueText.text = "물론이죠. 제가 도울 수 있는 일이 있을까요?";
        yield return new WaitForSeconds(2.5f);

        dialogueText.text = "정말 고맙네 우선은..";
        yield return new WaitForSeconds(2f);
        
        cameraMode = CutsceneCameraMode.ShrineFocus;
        dialogueText.text = "응..? 저건 무엇이지..?";
        yield return new WaitForSeconds(2f);
        
        dialogueText.text = "우리 섬에 이런 불길한 성소가 있다니.. 살면서 처음 보는 광경이군";
        yield return new WaitForSeconds(4f);

        dialogueText.text = "벌써 해야 할 일이 생긴것 같군. 미안하지만 저 성소를 조사해볼 수 있겠나?";
        yield return new WaitForSeconds(3f);

        dialogueText.text = "";
        dialogueText.gameObject.SetActive(false);

        dialogueFinished = true;
    }

    /* =======================
     * Cutscene End
     * ======================= */
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

    /* =======================
     * Letterbox
     * ======================= */
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
