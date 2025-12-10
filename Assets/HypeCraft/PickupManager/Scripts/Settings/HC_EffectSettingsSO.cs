namespace HC_Pickups
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;
    using System.Linq;

    // This ScriptableObject manages effects for objects in Unity.
    // It allows enabling/disabling effects, setting the effect index, and adding effects to a list.
    // The effect behavior can be applied to and stopped for target Transforms, and it uses coroutines to handle the effect logic.
    // The effect settings can be saved and updated in the Unity Editor.

    [CreateAssetMenu(fileName = "EffectSettings", menuName = "HC_ScriptableObjects/Settings/EffectSettings", order = 1)]
    public class HC_EffectSettingsSO : HC_AnimationBehaviourSO
    {
        public static event Action<bool> OnEffectEnabledChanged; // Event triggered when the effectEnabled value changes.

        public string headerText = "Effect Settings"; // Header text displayed in the Unity Editor.

        [Tooltip("Effect On/Off - Settings will only update when you toggle this Off/On")]
        public bool effectEnabled = true; // Determines whether the effects are enabled or disabled.

        [Tooltip("The index number corresponds to the effect in the effect list")]
        [Range(0, 100)] public int effectIndex = 0; // Index of the selected effect in the effect list.

        [Tooltip("Assign an HC_EffectListSO ScriptableObject here, which contains a list of effect prefabs to be used." +
            "\n" +
            "\nNOTE: By default, the system will automatically load an EffectList named 'EffectList' from the Resources folder." +
            "\nYou can override this by manually assigning another HC_EffectListSO here." +
            "\n" +
            "\nAdd your effect prefabs (particle system or VFX) to the assigned effect list.")]
        public HC_EffectListSO effectPrefabList; // Reference to the ScriptableObject containing the list of effect prefabs.

        [Tooltip("Show the names of the effects prefabs")]
        [SerializeField] List<GameObject> inspectorEffectPrefabs; // List of effect prefabs displayed in the Unity Inspector.

        List<GameObject> currentEffectInstances = new List<GameObject>(); // List of currently active effect instances.

        [HideInInspector] bool previousEffectEnabled = false; // Stores the previous state of the effectEnabled variable to detect changes.

        public bool EffectEnabled // When the effectEnabled value changes, it triggers the event every time
        {
            get => effectEnabled;
            set
            {
                if (effectEnabled != value)
                {
                    effectEnabled = value;
                    OnEffectEnabledChanged?.Invoke(effectEnabled); // Notify of change
                }
            }
        }

        private void Awake()
        {
            // Automatically assign a TestSO instance to effectPrefabList
            effectPrefabList = Resources.Load<HC_EffectListSO>("EffectList");
        }

        public void OnValidate()
        {
            // Update inspectorEffectPrefabs whenever effectPrefabList changes
            if (effectPrefabList != null && effectPrefabList.effectList != null)
            {
                if (inspectorEffectPrefabs == null)
                    inspectorEffectPrefabs = new List<GameObject>();
                inspectorEffectPrefabs.Clear();
                inspectorEffectPrefabs.AddRange(effectPrefabList.effectList);
                // Calp the index to the range of the effect list
                effectIndex = Mathf.Clamp(effectIndex, 0, effectPrefabList.effectList.Count - 1);
            }
            else
            {
                effectIndex = 0;
                if (inspectorEffectPrefabs != null)
                    inspectorEffectPrefabs.Clear();
            }

            //Update effectEnabled state if it changes
            if (effectEnabled != previousEffectEnabled)
            {
                previousEffectEnabled = effectEnabled;
                HandleEffectStateChange(effectEnabled);
            }
        }

        private void OnEnable()
        {
            OnEffectEnabledChanged += HandleEffectStateChange; // Register a listener

            // Clear the list to ensure no old references remain
            currentEffectInstances.Clear();

            if (effectPrefabList == null)
            {
                Debug.LogWarning("Effect Prefab List is missing! Please add an HC_EffectListSO ScriptableObject.");
                return;
            }
        }

        public void HandleEffectStateChange(bool isEnabled)
        {

            if (effectPrefabList == null || effectPrefabList.effectList.Count == 0)
            {
                Debug.LogError("EffectPrefabList is empty or null.");
                return;
            }

            // Remove all existing effects
            DeactivateEffects();

            // Ensure the list is empty before creating a new effect
            if (currentEffectInstances.Count > 0)
            {
                Debug.LogWarning("Effect instances were not fully cleared. Forcing cleanup.");
                currentEffectInstances.Clear();
            }

            if (isEnabled)
            {
                // Ensure that effectIndex is valid
                if (effectIndex < 0 || effectIndex >= effectPrefabList.effectList.Count)
                {
                    Debug.LogWarning("Effect index out of range. No effect will be instantiated.");
                    return;
                }
            }
        }

        public override void ApplyBehavior(Transform target, Dictionary<Transform, Coroutine> coroutineDict, MonoBehaviour runner)
        {
            // Implementation for applying behavior
        }

        public override void StopBehavior(Transform target, Dictionary<Transform, Coroutine> coroutineDict, MonoBehaviour runner)
        {
            DeactivateEffects();
        }

        public void SetEffectPosition(Vector3 position)
        {
            foreach (var prefab in effectPrefabList.effectList)
            {
                if (prefab != null)
                {
                    prefab.transform.position = position;
                }
            }
        }

        public void UpdateEffectSettings()
        {
            if (effectPrefabList == null || effectPrefabList.effectList.Count == 0)
            {
                Debug.LogError("EffectPrefabList is empty or null.");
                return;
            }

            if (effectEnabled != previousEffectEnabled)
            {
                if (effectEnabled)
                {
                    InstantiateEffectAtIndex(effectIndex);
                }
                previousEffectEnabled = effectEnabled; // Update previous state
            }
        }

        public GameObject InstantiateEffect(GameObject targetObject)
        {
            if (effectPrefabList == null)
            {
                Debug.LogError("EffectPrefabList is null. Please assign an HC_EffectListSO ScriptableObject.");
                return null;
            }
            if (effectPrefabList.effectList == null || effectPrefabList.effectList.Count == 0)
            {
                Debug.LogError("EffectPrefabList does not contain any prefabs. Please check that you have added at least one effect prefab to the Effect List.");
                return null;
            }

            if (effectIndex < 0 || effectIndex >= effectPrefabList.effectList.Count)
            {
                Debug.LogError("Effect index out of range.");
                return null;
            }

            // Ensure the targetObject is in objectsList
            bool isInObjectsList = HC_PickupsManager.Instance.pickupManagers
                .Where(pu => pu.objectsList != null) // Ensure objectsList is not null
                .SelectMany(pu => pu.objectsList)
                .Contains(targetObject);

            if (!isInObjectsList)
            {
                Debug.LogWarning($"Effect instantiation skipped for object not in objectsList: {targetObject.name}");
                return null;
            }

            // Instantiate the effect for the targetObject
            GameObject effectPrefab = effectPrefabList.effectList[effectIndex];
            GameObject instantiatedEffect = Instantiate(effectPrefab, targetObject.transform.position, Quaternion.Euler(-90, 0, 0));

            // Set the effect's parent to the targetObject
            instantiatedEffect.transform.SetParent(targetObject.transform);

            currentEffectInstances.Add(instantiatedEffect);
            return instantiatedEffect;
        }

        public GameObject InstantiateEffectAtIndex(int index)
        {
            if (effectPrefabList == null || effectPrefabList.effectList.Count == 0)
            {
                Debug.LogError("EffectPrefabList is empty or null.");
                return null;
            }

            if (index < 0 || index >= effectPrefabList.effectList.Count)
            {
                Debug.LogWarning("Effect index out of range.");
                return null;
            }

            // Remove any existing instances before creating a new one
            DeactivateEffects();

            GameObject effectPrefab = effectPrefabList.effectList[index];
            if (effectPrefab == null)
            {
                Debug.LogWarning("Effect prefab is null at index: " + index);
                return null;
            }

            GameObject instantiatedEffect = Instantiate(effectPrefab);
            instantiatedEffect.name = $"Effect_{effectPrefab.name}"; // Name the effect clearly
            currentEffectInstances.Add(instantiatedEffect); // Add the effect to the list
            return instantiatedEffect;
        }

        public void DeactivateEffects()
        {
            for (int i = currentEffectInstances.Count - 1; i >= 0; i--)
            {
                if (currentEffectInstances[i] != null)
                {
#if UNITY_EDITOR
                    if (!Application.isPlaying)
                    {
                        DestroyImmediate(currentEffectInstances[i]); // Remove the effect in edit mode
                    }
                    else
#endif
                    {
                        Destroy(currentEffectInstances[i]); // Remove the effect in play mode
                    }
                }
            }
            currentEffectInstances.Clear(); // Clear the list of references
        }

        private void OnDisable()
        {
            OnEffectEnabledChanged -= HandleEffectStateChange; // Remove listener

            // Clear the list to ensure no old references remain
            currentEffectInstances.Clear();
        }

        public void RemoveEffect(Transform target)
        {
            for (int i = currentEffectInstances.Count - 1; i >= 0; i--)
            {
                if (currentEffectInstances[i] != null)
                {
                    // Check if the effect's parent is the given target
                    if (currentEffectInstances[i].transform.parent == target)
                    {
                        Destroy(currentEffectInstances[i]); // Remove the effect
                        currentEffectInstances.RemoveAt(i); // Remove from the list
                    }
                }
            }
        }

        private void OnDestroy()
        {
            Debug.Log("OnDestroy called. Cleaning up effects...");
            DeactivateEffects(); // Remove all active effects
        }
    }
}
