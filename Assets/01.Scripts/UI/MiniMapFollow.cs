using UnityEngine;

public class MiniMapFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private float height = 20f;

    private void LateUpdate()
    {
        if (target == null) return;
        transform.position = new Vector3( target.position.x, height, target.position.z );
    }
}
