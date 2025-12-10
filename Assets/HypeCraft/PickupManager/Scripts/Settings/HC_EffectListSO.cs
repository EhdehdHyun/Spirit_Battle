namespace HC_Pickups
{
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// This ScriptableObject class manages a list of effect prefabs.
    /// It allows for the creation and management of a list of GameObject prefabs that can be used as effects in the game.
    /// The list can be edited in the Unity Editor, and the effects can be referenced and instantiated by other scripts.
    /// </summary>
    [CreateAssetMenu(fileName = "EffectList", menuName = "HC_ScriptableObjects/Settings/EffectList")]
    public class HC_EffectListSO : ScriptableObject
    {
        public List<GameObject> effectList;

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Initialize the list if null
            if (effectList == null)
            {
                effectList = new List<GameObject>();
                Debug.LogWarning("Effect List was initialized. Please add effect prefabs as needed.", this);
                return;
            }

            // Only warn about null elements instead of throwing errors
            bool hasNull = false;
            for (int i = 0; i < effectList.Count; i++)
            {
                if (effectList[i] == null)
                {
                    hasNull = true;
                }
            }

            if (hasNull)
            {
                Debug.LogWarning("Some elements in the Effect List are empty. Remember to assign GameObjects when needed.", this);
            }
            else if (effectList.Count == 0)
            {
                Debug.LogWarning("Effect List is empty. Please add at least one effect prefab when needed.", this);
            }
        }
#endif
    }
}