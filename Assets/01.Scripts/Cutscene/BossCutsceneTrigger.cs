using System.Collections;
using UnityEngine;

public class BossCutsceneTrigger : MonoBehaviour
{
    [Header("�ʼ�")]
    [SerializeField] private Camera mainCamera;       // ���� �÷��� ī�޶�
    [SerializeField] private Camera cutsceneCamera;   // CutsceneCamera
    [SerializeField] private Transform lookTarget;    // ����/��������ũ �� �ٶ� ���

    [Header("�ƾ� ī�޶� ����/�� ����")]
    [SerializeField] private Transform camStart;
    [SerializeField] private Transform camEnd;

    [Header("����")]
    [SerializeField] private float duration = 2.0f;   // �̵� �ð�
    [SerializeField] private bool freezeTimeScale = true;
    [SerializeField] private MonoBehaviour[] disableWhileCutscene; // �÷��̾� �Է�/���� ��ũ��Ʈ ��

    private bool played;
    private float prevTimeScale = 1f;

    private void Reset()
    {
        // Ʈ���ŷ� ���� ���ϰ�
        var col = GetComponent<Collider>();
        if (col) col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (played) return;
        if (!other.CompareTag("Player")) return;

        played = true;
        StartCoroutine(CoPlay());
    }

    private IEnumerator CoPlay()
    {
        if (!mainCamera) mainCamera = Camera.main;
        if (!mainCamera || !cutsceneCamera || !camStart || !camEnd)
        {
            yield break;
        }

        // 1) �Է� ��� + (����) �ð�����
        foreach (var s in disableWhileCutscene)
            if (s) s.enabled = false;

        if (freezeTimeScale)
        {
            prevTimeScale = Time.timeScale;
            Time.timeScale = 0f;
        }

        // 2) ī�޶� ��ȯ
        mainCamera.gameObject.SetActive(false);
        cutsceneCamera.gameObject.SetActive(true);

        // 3) �ƾ� ī�޶� ���� ����
        cutsceneCamera.transform.position = camStart.position;
        cutsceneCamera.transform.rotation = camStart.rotation;

        // TimeScale=0 �̾ ����ǰ� UnscaledTime ���
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float a = Mathf.Clamp01(t / duration);

            cutsceneCamera.transform.position = Vector3.Lerp(camStart.position, camEnd.position, a);

            if (lookTarget)
            {
                Vector3 dir = (lookTarget.position - cutsceneCamera.transform.position);
                if (dir.sqrMagnitude > 0.0001f)
                    cutsceneCamera.transform.rotation = Quaternion.Slerp(
                        camStart.rotation,
                        Quaternion.LookRotation(dir.normalized, Vector3.up),
                        a
                    );
            }
            else
            {
                cutsceneCamera.transform.rotation = Quaternion.Slerp(camStart.rotation, camEnd.rotation, a);
            }

            yield return null;
        }

        // 4) ����
        cutsceneCamera.gameObject.SetActive(false);
        mainCamera.gameObject.SetActive(true);

        if (freezeTimeScale)
            Time.timeScale = prevTimeScale;

        foreach (var s in disableWhileCutscene)
            if (s) s.enabled = true;
    }
}
