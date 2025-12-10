using UnityEngine;

public class LegacyMove : MonoBehaviour
{
    public float moveSpeed = 5f;

    private void Update()
    {
        // 옛날 Input Manager 축 사용 (Horizontal / Vertical)
        float h = Input.GetAxisRaw("Horizontal"); // A,D / 좌우 화살표
        float v = Input.GetAxisRaw("Vertical");   // W,S / 상하 화살표

        Vector3 dir = new Vector3(h, 0f, v);

        if (dir.sqrMagnitude > 1f)
            dir.Normalize();

        transform.Translate(dir * moveSpeed * Time.deltaTime, Space.World);
    }
}
