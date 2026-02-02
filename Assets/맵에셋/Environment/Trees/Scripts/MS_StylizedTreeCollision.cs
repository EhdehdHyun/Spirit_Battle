using UnityEngine;

namespace SoStylized
{
	public class MS_StylizedTreeCollision : MonoBehaviour
	{
		void Awake()
		{
#if UNITY_EDITOR
			// Skip if this is the prefab asset itself (not an instance)
			if (UnityEditor.PrefabUtility.IsPartOfPrefabAsset(gameObject))
			{
				return;
			}
#endif

			// Only check on this GameObject (since script is on LOD0)
			CapsuleCollider col = GetComponent<CapsuleCollider>();
			if (col != null)
			{
#if UNITY_EDITOR
				if (!Application.isPlaying)
				{
					DestroyImmediate(col);
				}
				else
#endif
				{
					Destroy(col);
				}
			}
		}
	}
}