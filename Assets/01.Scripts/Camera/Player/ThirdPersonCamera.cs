using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ThirdPersonCamera : MonoBehaviour
{
    public Transform target;
    public Vector3 pivotOffset = new Vector3(0f, 1.6f, 0f);

    public float distance = 3.5f;
    public float minDistance = 0.6f;
    public float collisionRadius = 0.18f;
    public LayerMask collisionMask = ~0;

    public float sensitivity = 0.12f;
    public float minPitch = -35f;
    public float maxPitch = 70f;
    public float rotationLerp = 18f;
    public float followLerp = 20f;

    float yaw;
    float pitch;
    Vector2 lookInput;

    public void SetLookInput(Vector2 look) => lookInput = look;

    public Vector3 PlanarForward
    {
        get
        {
            Vector3 f = transform.forward;
            f.y = 0f;
            return f.sqrMagnitude < 0.0001f ? Vector3.forward : f.normalized;
        }
    }

    public Vector3 PlanarRight
    {
        get
        {
            Vector3 r = transform.right;
            r.y = 0f;
            return r.sqrMagnitude < 0.0001f ? Vector3.right : r.normalized;
        }
    }

    void LateUpdate()
    {
        if (target == null) return;

        yaw += lookInput.x * sensitivity;
        pitch -= lookInput.y * sensitivity;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        Quaternion desiredRot = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 pivot = target.position + pivotOffset;

        Vector3 idealPos = pivot + desiredRot * new Vector3(0, 0, -distance);

        float correctedDist = distance;
        Vector3 dir = (idealPos - pivot).normalized;

        if (Physics.SphereCast(pivot, collisionRadius, dir, out RaycastHit hit,
            distance, collisionMask, QueryTriggerInteraction.Ignore))
        {
            correctedDist = Mathf.Max(minDistance, hit.distance - 0.05f);
        }

        Vector3 desiredPos = pivot + desiredRot * new Vector3(0, 0, -correctedDist);

        transform.position = Vector3.Lerp(transform.position, desiredPos, followLerp * Time.deltaTime);
        transform.rotation = Quaternion.Slerp(transform.rotation, desiredRot, rotationLerp * Time.deltaTime);
    }
}