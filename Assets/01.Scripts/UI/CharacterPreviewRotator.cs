using UnityEngine;

public class CharacterPreviewRotator : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private float rotateSpeed = 10f;

    private float targetAngle;

    public void SetView(int dir)
    {
        targetAngle = dir * 90f;
    }

    private void Update()
    {
        if (target == null) return;

        float currentY = target.eulerAngles.y;
        float newY = Mathf.LerpAngle(currentY, targetAngle, Time.deltaTime * rotateSpeed);
        target.rotation = Quaternion.Euler(0, newY, 0);
    }
}
