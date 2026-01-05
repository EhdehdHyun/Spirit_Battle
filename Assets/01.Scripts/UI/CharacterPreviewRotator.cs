using UnityEngine;

public class CharacterPreviewRotator : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private float rotateSpeed = 10f;

    private int directionIndex = 0;
    private float targetAngle;

    public void RotateRight()
    {
        target.rotation *= Quaternion.Euler(0, 90f, 0);
    }

    public void RotateLeft()
    {
        target.rotation *= Quaternion.Euler(0, -90f, 0);
    }

    private void Update()
    {
        if (target == null) return;

        float currentY = target.eulerAngles.y;
        float newY = Mathf.LerpAngle(currentY, targetAngle, Time.deltaTime * rotateSpeed);
        target.rotation = Quaternion.Euler(0, newY, 0);
    }
}