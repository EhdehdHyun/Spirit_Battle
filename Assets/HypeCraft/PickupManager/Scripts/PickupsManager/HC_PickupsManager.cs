namespace HC_Pickups
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using UnityEditor;
    using UnityEngine;

    // This class manages the pickup system in the game, including animations, effects, and audio for collectable objects.
    // It allows adding and removing pickup managers, initializing and updating animations, and handling effects.
    // The class uses ScriptableObjects to manage settings for float animations, rotation animations.
    // It also provides methods to apply and stop behaviors, update animations, and manage coroutines for animations.

    [Serializable]
    public class PickupGroup
    {
        #region Variables
        [Tooltip("Give the object group a descriptive name")]
        public string headerText = "Group";
        // A descriptive name for the group of objects, used for organizational purposes.

        [Tooltip("Drag the game objects you want to edit here." +
            "\n" +
            "\nNote! Only objects with the HC_Collectable code attached can be added to these lists, so that you can edit their settings."),
            CollectableOnly]

        public List<GameObject> objectsList;
        // A list of GameObjects that belong to this group and will be affected by animations or effects.

        public HC_FloatAnimationSettingsSO floatAnimation;
        // ScriptableObject that defines the floating animation settings for the objects in this group.

        public HC_RotationAnimationSettingsSO rotationAnimation;
        // ScriptableObject that defines the rotation animation settings for the objects in this group.

        public HC_EffectSettingsSO effectSettings;
        // ScriptableObject that defines the effect settings (e.g., visual effects) for the objects in this group.

        public Dictionary<Transform, Vector3> originalPositions = new Dictionary<Transform, Vector3>();
        // A dictionary to store the original positions of the objects in this group, used to reset their positions.

        public Dictionary<Transform, Quaternion> originalRotations = new Dictionary<Transform, Quaternion>();
        // A dictionary to store the original rotations of the objects in this group, used to reset their rotations.

        public Dictionary<Transform, Coroutine> rotateCoroutines = new Dictionary<Transform, Coroutine>();
        // A dictionary to track active coroutines for rotation animations, mapped to the corresponding object transforms.

        public Dictionary<Transform, Coroutine> floatCoroutines = new Dictionary<Transform, Coroutine>();
        // A dictionary to track active coroutines for floating animations, mapped to the corresponding object transforms.
        #endregion
    }

    [Serializable]
    [RequireComponent(typeof(HC_AudioSettings))] // This component requires an AudioSettings component and will add it if not present
    public class HC_PickupsManager : MonoBehaviour
    {
        #region Singleton

        /// <summary>
        /// Singleton instance of the HC_PickupsManager.
        /// Ensures only one instance exists in the scene.
        /// </summary>
        public static HC_PickupsManager Instance { get; private set; }

        #endregion

        #region Pickup Management

        [Tooltip("List of Pickup Managers instances")]
        public List<PickupGroup> pickupManagers = new List<PickupGroup>();

        #endregion

        #region Animation Settings

        public HC_FloatAnimationSettingsSO floatAnimation; // Global float animation settings
        public HC_RotationAnimationSettingsSO rotationAnimation; // Global rotation animation settings
        public HC_EffectSettingsSO effectSettings; // Global effect settings

        #endregion

        #region Internal State

        private MonoBehaviour runner; // Reference to the MonoBehaviour that runs coroutines
        public HC_AudioSettings audioSettings; // Reference to the AudioSettings component

        protected Dictionary<Transform, Vector3> originalPositions = new Dictionary<Transform, Vector3>();
        protected Dictionary<Transform, Quaternion> originalRotations = new Dictionary<Transform, Quaternion>();
        protected Dictionary<Transform, Coroutine> rotateCoroutines = new Dictionary<Transform, Coroutine>();
        protected Dictionary<Transform, Coroutine> floatCoroutines = new Dictionary<Transform, Coroutine>();

        #endregion

        #region Unity Lifecycle Methods

        private void Awake()
        {
            // Ensure only one instance exists
            if (Instance != null)
            {
                Debug.LogError($"Multiple instances of {nameof(HC_PickupsManager)} detected. Destroying duplicate on {gameObject.name}.");
                Destroy(gameObject);
                return;
            }
            Instance = this;

            runner = this; // Set the runner to this MonoBehaviour

            // Initialize audio settings
            if (!TryGetComponent(out audioSettings))
            {
                Debug.LogError($"Missing {nameof(HC_AudioSettings)} component on {gameObject.name}. Please add it.");
            }

            // Load global and per-group ScriptableObjects
            LoadGlobalScriptableObjects();
            LoadScriptableObjectsForPickUpManagers();
        }

        private void Start()
        {
            if (pickupManagers == null || pickupManagers.Count == 0)
            {
                Debug.LogError("The pickupManagers list is empty. Please add at least one Pickup Manager.");
                return;
            }

            InitializeObjectsList(); // Initialize the objects for animation
        }

        private void OnValidate()
        {
            // Ensure audio settings are initialized
            if (!TryGetComponent(out audioSettings))
            {
                Debug.LogError($"Missing {nameof(HC_AudioSettings)} component on {gameObject.name}. Please add it.");
            }

            // Automatically reload ScriptableObjects when changes are made in the editor
            LoadGlobalScriptableObjects();
            LoadScriptableObjectsForPickUpManagers();
        }

        private void OnEnable()
        {
            if (pickupManagers == null || pickupManagers.Count == 0)
            {
                Debug.LogWarning("The pickupManagers list is empty. Please add at least one Pickup Manager.");
            }
        }

        private void Update()
        {
            if (pickupManagers == null || pickupManagers.Count == 0)
            {
                return;
            }

            foreach (var pickupGroup in pickupManagers)
            {
                // Updates animations when controlled animations are toggled on/off
                var objectTransforms = pickupGroup.objectsList
                    .Where(obj => obj != null)
                    .Select(obj => obj.transform)
                    .ToList();

                UpdateAnimations(pickupGroup, objectTransforms, animationUpdate: true);
            }
        }

        private void OnDisable()
        {
            // Deactivate effects when the manager is disabled
            foreach (var pickupGroup in pickupManagers)
            {
                pickupGroup.effectSettings?.DeactivateEffects();
            }
        }

        #endregion

        #region Pickup Management Methods

        /// <summary>
        /// Adds a new Pickup Manager with default settings.
        /// </summary>
        public void AddNewPickupManager()
        {
            // Get the next index which should match the new pickup manager's position
            int nextIndex = pickupManagers.Count + 1;

            var newPickupGroup = new PickupGroup
            {
                floatAnimation = CreateNewFloatAnimationSettingsWithIndex(nextIndex),
                rotationAnimation = CreateNewRotationAnimationSettingsWithIndex(nextIndex),
                effectSettings = CreateNewEffectSettingsWithIndex(nextIndex)
            };
            pickupManagers.Add(newPickupGroup);
        }


        /// <summary>
        /// Removes the last Pickup Manager from the list.
        /// </summary>
        public void RemoveLastPickupManager()
        {
            if (pickupManagers.Count == 0)
            {
                Debug.LogWarning("No PickupGroup to remove.");
                return;
            }

            var removedGroup = pickupManagers[^1]; // Use index from the end
            pickupManagers.RemoveAt(pickupManagers.Count - 1);

            ValidateEffectSettings();
        }

        /// <summary>
        /// Validates the effect settings of all Pickup Managers.
        /// Logs a warning if any Pickup Manager has missing effect settings or prefab lists.
        /// </summary>
        private void ValidateEffectSettings()
        {
            foreach (var pickupGroup in pickupManagers)
            {
                if (pickupGroup.effectSettings == null)
                {
                    Debug.LogWarning($"PickupGroup '{pickupGroup.headerText}' is missing effect settings.");
                    continue;
                }

                if (pickupGroup.effectSettings.effectPrefabList == null ||
                    pickupGroup.effectSettings.effectPrefabList.effectList == null ||
                    pickupGroup.effectSettings.effectPrefabList.effectList.Count == 0)
                {
                    Debug.LogWarning($"PickupGroup '{pickupGroup.headerText}' has an empty or missing effect prefab list.");
                }
            }
        }

#if UNITY_EDITOR
        /// <summary>
        /// Returns the next available index for a settings asset of the given type.
        /// Scans the Resources folder for existing assets named "Settings_{settingsType} X.asset"
        /// and returns the next free number (max + 1).
        /// This ensures that each new settings asset is uniquely named and not scene-specific.
        /// </summary>
        /// <param name="settingsType">The type of the settings (e.g., "Float", "Rotation", "Effect").</param>
        /// <returns>The next available index for the settings asset.</returns>
        private int GetNextSettingsIndex(string settingsType)
        {
            string resourcesPath = "Assets/HypeCraft/PickupManager/Resources";
            var guids = AssetDatabase.FindAssets($"t:ScriptableObject", new[] { resourcesPath });
            int maxIndex = 0;
            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string fileName = System.IO.Path.GetFileNameWithoutExtension(path);
                if (fileName.StartsWith($"Settings_{settingsType}"))
                {
                    // Try to parse the last part of the filename as an integer index
                    var parts = fileName.Split(' ');
                    if (parts.Length > 1 && int.TryParse(parts[^1], out int number))
                    {
                        if (number > maxIndex)
                            maxIndex = number;
                    }
                }
            }
            return maxIndex + 1;
        }
#endif

        /// <summary>
        /// Creates a new FloatAnimationSettings asset with a unique, incrementing name.
        /// The asset is named "Settings_Float X.asset", where X is the next available number.
        /// This avoids scene-specific naming and ensures uniqueness across the project.
        /// </summary>
        private HC_FloatAnimationSettingsSO CreateNewFloatAnimationSettings()
        {
            HC_FloatAnimationSettingsSO newFloatAnimation = ScriptableObject.CreateInstance<HC_FloatAnimationSettingsSO>();
#if UNITY_EDITOR
            int nextIndex = GetNextSettingsIndex("Float");
            newFloatAnimation.headerText = $"Settings_Float {nextIndex}";
            string resourcesPath = "Assets/Resources";
            if (!AssetDatabase.IsValidFolder(resourcesPath))
            {
                AssetDatabase.CreateFolder("Assets", "Resources");
            }
            string assetPath = $"{resourcesPath}/Settings_Float {nextIndex}.asset";
            AssetDatabase.CreateAsset(newFloatAnimation, assetPath);
            AssetDatabase.SaveAssets();
#endif
            return newFloatAnimation;
        }

        /// <summary>
        /// Creates a new FloatAnimationSettings asset with the specified index.
        /// </summary>
        /// <param name="index">The specific index to use for the settings asset.</param>
        /// <returns>The newly created FloatAnimationSettings ScriptableObject.</returns>
        private HC_FloatAnimationSettingsSO CreateNewFloatAnimationSettingsWithIndex(int index)
        {
            HC_FloatAnimationSettingsSO newFloatAnimation = ScriptableObject.CreateInstance<HC_FloatAnimationSettingsSO>();
#if UNITY_EDITOR
            newFloatAnimation.headerText = $"Settings_Float {index}";
            string resourcesPath = "Assets/HypeCraft/PickupManager/Resources";
            if (!AssetDatabase.IsValidFolder(resourcesPath))
            {
                // Create folder hierarchy if needed
                if (!AssetDatabase.IsValidFolder("Assets/HypeCraft"))
                    AssetDatabase.CreateFolder("Assets", "HypeCraft");
                if (!AssetDatabase.IsValidFolder("Assets/HypeCraft/PickupManager"))
                    AssetDatabase.CreateFolder("Assets/HypeCraft", "PickupManager");
                AssetDatabase.CreateFolder("Assets/HypeCraft/PickupManager", "Resources");
            }
            string assetPath = $"{resourcesPath}/Settings_Float {index}.asset";
            AssetDatabase.CreateAsset(newFloatAnimation, assetPath);
            AssetDatabase.SaveAssets();
#endif
            return newFloatAnimation;
        }

        /// <summary>
        /// Creates a new RotationAnimationSettings asset with a unique, incrementing name.
        /// The asset is named "Settings_Rotation X.asset", where X is the next available number.
        /// This avoids scene-specific naming and ensures uniqueness across the project.
        /// </summary>
        private HC_RotationAnimationSettingsSO CreateNewRotationAnimationSettings()
        {
            HC_RotationAnimationSettingsSO newRotationAnimation = ScriptableObject.CreateInstance<HC_RotationAnimationSettingsSO>();
#if UNITY_EDITOR
            int nextIndex = GetNextSettingsIndex("Rotation");
            newRotationAnimation.headerText = $"Settings_Rotation {nextIndex}";
            string resourcesPath = "Assets/HypeCraft/PickupManager/Resources";
            if (!AssetDatabase.IsValidFolder(resourcesPath))
            {
                // Create folder hierarchy if needed
                if (!AssetDatabase.IsValidFolder("Assets/HypeCraft"))
                    AssetDatabase.CreateFolder("Assets", "HypeCraft");
                if (!AssetDatabase.IsValidFolder("Assets/HypeCraft/PickupManager"))
                    AssetDatabase.CreateFolder("Assets/HypeCraft", "PickupManager");
                AssetDatabase.CreateFolder("Assets/HypeCraft/PickupManager", "Resources");
            }
            string assetPath = $"{resourcesPath}/Settings_Rotation {nextIndex}.asset";
            AssetDatabase.CreateAsset(newRotationAnimation, assetPath);
            AssetDatabase.SaveAssets();
#endif
            return newRotationAnimation;
        }

        /// <summary>
        /// Creates a new RotationAnimationSettings asset with the specified index.
        /// </summary>
        /// <param name="index">The specific index to use for the settings asset.</param>
        /// <returns>The newly created RotationAnimationSettings ScriptableObject.</returns>
        private HC_RotationAnimationSettingsSO CreateNewRotationAnimationSettingsWithIndex(int index)
        {
            HC_RotationAnimationSettingsSO newRotationAnimation = ScriptableObject.CreateInstance<HC_RotationAnimationSettingsSO>();
#if UNITY_EDITOR
            newRotationAnimation.headerText = $"Settings_Rotation {index}";
            string resourcesPath = "Assets/HypeCraft/PickupManager/Resources";
            if (!AssetDatabase.IsValidFolder(resourcesPath))
            {
                // Create folder hierarchy if needed
                if (!AssetDatabase.IsValidFolder("Assets/HypeCraft"))
                    AssetDatabase.CreateFolder("Assets", "HypeCraft");
                if (!AssetDatabase.IsValidFolder("Assets/HypeCraft/PickupManager"))
                    AssetDatabase.CreateFolder("Assets/HypeCraft", "PickupManager");
                AssetDatabase.CreateFolder("Assets/HypeCraft/PickupManager", "Resources");
            }
            string assetPath = $"{resourcesPath}/Settings_Rotation {index}.asset";
            AssetDatabase.CreateAsset(newRotationAnimation, assetPath);
            AssetDatabase.SaveAssets();
#endif
            return newRotationAnimation;
        }

        /// <summary>
        /// Creates a new EffectSettings asset with a unique, incrementing name.
        /// The asset is named "Settings_Effect X.asset", where X is the next available number.
        /// This avoids scene-specific naming and ensures uniqueness across the project.
        /// If a default EffectList is found, it is assigned automatically.
        /// </summary>
        private HC_EffectSettingsSO CreateNewEffectSettings()
        {
            HC_EffectSettingsSO newEffectSettings = ScriptableObject.CreateInstance<HC_EffectSettingsSO>();
#if UNITY_EDITOR
            int nextIndex = GetNextSettingsIndex("Effect");
            newEffectSettings.headerText = $"Settings_Effect {nextIndex}";
            string resourcesPath = "Assets/HypeCraft/PickupManager/Resources";
            if (!AssetDatabase.IsValidFolder(resourcesPath))
            {
                // Create folder hierarchy if needed
                if (!AssetDatabase.IsValidFolder("Assets/HypeCraft"))
                    AssetDatabase.CreateFolder("Assets", "HypeCraft");
                if (!AssetDatabase.IsValidFolder("Assets/HypeCraft/PickupManager"))
                    AssetDatabase.CreateFolder("Assets", "PickupManager");
                AssetDatabase.CreateFolder("Assets/HypeCraft/PickupManager", "Resources");
            }
            string assetPath = $"{resourcesPath}/Settings_Effect {nextIndex}.asset";
            AssetDatabase.CreateAsset(newEffectSettings, assetPath);

            // Assign default EffectList if available
            var defaultEffectList = Resources.Load<HC_EffectListSO>("EffectList");
            if (defaultEffectList != null)
            {
                newEffectSettings.effectPrefabList = defaultEffectList;
                newEffectSettings.OnValidate();
                EditorUtility.SetDirty(newEffectSettings);
            }

            AssetDatabase.SaveAssets();
#endif
            return newEffectSettings;
        }

        /// <summary>
        /// Creates a new EffectSettings asset with the specified index.
        /// </summary>
        /// <param name="index">The specific index to use for the settings asset.</param>
        /// <returns>The newly created EffectSettings ScriptableObject.</returns>
        private HC_EffectSettingsSO CreateNewEffectSettingsWithIndex(int index)
        {
            HC_EffectSettingsSO newEffectSettings = ScriptableObject.CreateInstance<HC_EffectSettingsSO>();
#if UNITY_EDITOR
            newEffectSettings.headerText = $"Settings_Effect {index}";
            string resourcesPath = "Assets/HypeCraft/PickupManager/Resources";
            if (!AssetDatabase.IsValidFolder(resourcesPath))
            {
                // Create folder hierarchy if needed
                if (!AssetDatabase.IsValidFolder("Assets/HypeCraft"))
                    AssetDatabase.CreateFolder("Assets", "HypeCraft");
                if (!AssetDatabase.IsValidFolder("Assets/HypeCraft/PickupManager"))
                    AssetDatabase.CreateFolder("Assets", "PickupManager");
                AssetDatabase.CreateFolder("Assets/HypeCraft/PickupManager", "Resources");
            }
            string assetPath = $"{resourcesPath}/Settings_Effect {index}.asset";
            AssetDatabase.CreateAsset(newEffectSettings, assetPath);

            // Assign default EffectList if available
            var defaultEffectList = Resources.Load<HC_EffectListSO>("EffectList");
            if (defaultEffectList != null)
            {
                newEffectSettings.effectPrefabList = defaultEffectList;
                newEffectSettings.OnValidate();
                EditorUtility.SetDirty(newEffectSettings);
            }

            AssetDatabase.SaveAssets();
#endif
            return newEffectSettings;
        }

        private void LoadGlobalScriptableObjects()
        {
            floatAnimation = LoadScriptableObject(floatAnimation, "Settings_Float 1");
            rotationAnimation = LoadScriptableObject(rotationAnimation, "Settings_Rotation 1");
            effectSettings = LoadScriptableObject(effectSettings, "Settings_Effect 1");
        }

        private void LoadScriptableObjectsForPickUpManagers()
        {
            for (int i = 0; i < pickupManagers.Count; i++)
            {
                var pickupGroup = pickupManagers[i];
                // Changed resource naming pattern to match the actual file names
                pickupGroup.floatAnimation ??= LoadScriptableObject(pickupGroup.floatAnimation, $"Settings_Float {i + 1}");
                pickupGroup.rotationAnimation ??= LoadScriptableObject(pickupGroup.rotationAnimation, $"Settings_Rotation {i + 1}");
                pickupGroup.effectSettings ??= LoadScriptableObject(pickupGroup.effectSettings, $"Settings_Effect {i + 1}");
            }
        }

        private T LoadScriptableObject<T>(T currentObject, string resourceName) where T : ScriptableObject
        {
            if (currentObject == null)
            {
                T loadedObject = Resources.Load<T>(resourceName);
                if (loadedObject == null)
                {
                    Debug.LogWarning($"{typeof(T).Name} not found in Resources! Ensure '{resourceName}' exists.");
                }
                return loadedObject;
            }
            return currentObject;
        }

        #endregion

        #region Animation Management Methods

        /// <summary>
        /// Initializes the objects in all PickupGroups by storing their original positions and rotations
        /// and applying animations if enabled.
        /// </summary>
        private void InitializeObjectsList()
        {
            foreach (var pickupGroup in pickupManagers)
            {
                // Check if the objectsList is null or empty
                if (pickupGroup.objectsList == null || pickupGroup.objectsList.Count == 0)
                {
                    Debug.LogWarning($"PickupGroup '{pickupGroup.headerText}' has an empty or null objects list.");
                    continue;
                }

                // Filter out null objects and get their transforms
                var objectTransforms = pickupGroup.objectsList
                    .Where(obj => obj != null)
                    .Select(obj => obj.transform)
                    .ToList();

                // Store the original positions and rotations of the objects
                foreach (var objTransform in objectTransforms)
                {
                    if (!pickupGroup.originalPositions.ContainsKey(objTransform))
                    {
                        pickupGroup.originalPositions[objTransform] = objTransform.position;
                    }

                    if (!pickupGroup.originalRotations.ContainsKey(objTransform))
                    {
                        pickupGroup.originalRotations[objTransform] = objTransform.rotation;
                    }
                }

                // Apply animations to the objects
                UpdateAnimations(pickupGroup, objectTransforms, animationUpdate: true);
            }
        }

        /// <summary>
        /// Applies the given animation behavior to the target Transform.
        /// </summary>
        /// <param name="behavior">The animation behavior to apply.</param>
        /// <param name="target">The Transform to which the behavior will be applied.</param>
        /// <param name="coroutineDict">A dictionary to track active coroutines for the target Transform.</param>
        public void ApplyBehavior(HC_AnimationBehaviourSO behavior, Transform target, Dictionary<Transform, Coroutine> coroutineDict)
        {
            behavior.ApplyBehavior(target, coroutineDict, this);
        }

        /// <summary>
        /// Stops the given animation behavior for the target Transform.
        /// </summary>
        /// <param name="behavior">The animation behavior to stop.</param>
        /// <param name="target">The Transform for which the behavior will be stopped.</param>
        /// <param name="coroutineDict">A dictionary to track active coroutines for the target Transform.</param>
        public void StopBehavior(HC_AnimationBehaviourSO behavior, Transform target, Dictionary<Transform, Coroutine> coroutineDict)
        {
            behavior.StopBehavior(target, coroutineDict, this);
        }

        /// <summary>
        /// Updates animations for a given PickupGroup and its associated object transforms.
        /// </summary>
        /// <param name="pickupGroup">The PickupGroup to update animations for.</param>
        /// <param name="objectTransforms">The list of object transforms to animate.</param>
        /// <param name="animationUpdate">Whether to force an animation update.</param>
        public void UpdateAnimations(PickupGroup pickupGroup, List<Transform> objectTransforms, bool animationUpdate)
        {
            foreach (var objTransform in objectTransforms)
            {
                if (objTransform == null) continue;

                // Manage float animation
                if (pickupGroup.floatAnimation.floatEnabled && !pickupGroup.floatCoroutines.ContainsKey(objTransform))
                {
                    HandleFloatingAnimation(pickupGroup, objTransform, animationUpdate);
                }
                else if (!pickupGroup.floatAnimation.floatEnabled && pickupGroup.floatCoroutines.ContainsKey(objTransform))
                {
                    StopAndResetCoroutine(pickupGroup.floatCoroutines, objTransform, pickupGroup);
                }

                // Manage rotation animation
                if (pickupGroup.rotationAnimation.rotationEnabled && !pickupGroup.rotateCoroutines.ContainsKey(objTransform))
                {
                    HandleRotationAnimation(pickupGroup, objTransform, animationUpdate);
                }
                else if (!pickupGroup.rotationAnimation.rotationEnabled && pickupGroup.rotateCoroutines.ContainsKey(objTransform))
                {
                    StopAndRemoveCoroutine(pickupGroup.rotateCoroutines, objTransform);
                    objTransform.rotation = pickupGroup.originalRotations.ContainsKey(objTransform)
                        ? pickupGroup.originalRotations[objTransform]
                        : Quaternion.identity; // Reset rotation
                }

                // Handle effect instantiation and updating
                if (pickupGroup.effectSettings != null && pickupGroup.effectSettings.effectEnabled)
                {
                    HandleEffectInstantiation(pickupGroup, objTransform);
                }
            }
        }

        /// <summary>
        /// Handles floating animation for a given Transform.
        /// </summary>
        /// <param name="pickupGroup">The PickupGroup containing the animation settings.</param>
        /// <param name="objTransform">The Transform to animate.</param>
        /// <param name="forceUpdate">Whether to force an update of the animation.</param>
        private void HandleFloatingAnimation(PickupGroup pickupGroup, Transform objTransform, bool forceUpdate)
        {
            if (pickupGroup.floatAnimation.floatEnabled && pickupGroup.floatAnimation.floatMovementSpeed > 0 && pickupGroup.floatAnimation.floatDistance > 0)
            {
                if (!pickupGroup.floatCoroutines.ContainsKey(objTransform) || forceUpdate)
                {
                    StopBehavior(pickupGroup.floatAnimation, objTransform, pickupGroup.floatCoroutines);
                    Vector3 initialPosition = pickupGroup.originalPositions.ContainsKey(objTransform)
                        ? pickupGroup.originalPositions[objTransform]
                        : objTransform.position;
                    objTransform.position = initialPosition + Vector3.up * pickupGroup.floatAnimation.floatBaseHeight;
                    pickupGroup.floatCoroutines[objTransform] = StartCoroutine(pickupGroup.floatAnimation.FloatObject(objTransform, runner, initialPosition));
                    pickupGroup.floatAnimation.SaveChanges();
                }
            }
            else if (!pickupGroup.floatAnimation.floatEnabled && pickupGroup.floatCoroutines.ContainsKey(objTransform))
            {
                StopAndResetCoroutine(pickupGroup.floatCoroutines, objTransform, pickupGroup);
            }
        }

        /// <summary>
        /// Handles rotation animation for a given Transform.
        /// </summary>
        /// <param name="pickupGroup">The PickupGroup containing the animation settings.</param>
        /// <param name="objTransform">The Transform to animate.</param>
        /// <param name="forceUpdate">Whether to force an update of the animation.</param>
        private void HandleRotationAnimation(PickupGroup pickupGroup, Transform objTransform, bool forceUpdate)
        {
            if (pickupGroup.rotationAnimation.rotationEnabled && pickupGroup.rotationAnimation.rotationMovementSpeed > 0)
            {
                objTransform.rotation = pickupGroup.originalRotations.ContainsKey(objTransform)
                    ? pickupGroup.originalRotations[objTransform]
                    : objTransform.rotation;

                if (!pickupGroup.rotateCoroutines.ContainsKey(objTransform) || forceUpdate)
                {
                    StopBehavior(pickupGroup.rotationAnimation, objTransform, pickupGroup.rotateCoroutines);
                    ApplyBehavior(pickupGroup.rotationAnimation, objTransform, pickupGroup.rotateCoroutines);
                }
            }
            else if (!pickupGroup.rotationAnimation.rotationEnabled && pickupGroup.rotateCoroutines.ContainsKey(objTransform))
            {
                StopAndRemoveCoroutine(pickupGroup.rotateCoroutines, objTransform);
                objTransform.localRotation = pickupGroup.originalRotations.ContainsKey(objTransform)
                    ? pickupGroup.originalRotations[objTransform]
                    : Quaternion.identity; // Reset rotation
            }
        }

        /// <summary>
        /// Handles the instantiation of effects for a given Transform.
        /// </summary>
        /// <param name="pickupGroup">The PickupGroup containing the effect settings.</param>
        /// <param name="objTransform">The Transform to attach the effect to.</param>
        private void HandleEffectInstantiation(PickupGroup pickupGroup, Transform objTransform)
        {
            bool hasEffect = objTransform.GetComponentsInChildren<Transform>()
                .Any(child => child.name == $"Effect_{objTransform.name}");

            if (!hasEffect)
            {
                GameObject instantiatedEffect = pickupGroup.effectSettings.InstantiateEffect(objTransform.gameObject);
                if (instantiatedEffect != null)
                {
                    instantiatedEffect.name = $"Effect_{objTransform.name}";

                    // Ensure the instantiated effect has the same scale as the prefab
                    GameObject effectPrefab = pickupGroup.effectSettings.effectPrefabList.effectList[pickupGroup.effectSettings.effectIndex];
                    if (effectPrefab != null)
                    {
                        instantiatedEffect.transform.localScale = effectPrefab.transform.localScale;
                    }
                }
            }
        }

        #endregion

        #region Coroutine Management Methods

        /// <summary>
        /// Stops and removes a coroutine for a given Transform.
        /// </summary>
        /// <param name="coroutineDict">The dictionary tracking active coroutines.</param>
        /// <param name="objTransform">The Transform whose coroutine should be stopped.</param>
        private void StopAndRemoveCoroutine(Dictionary<Transform, Coroutine> coroutineDict, Transform objTransform)
        {
            if (coroutineDict.ContainsKey(objTransform))
            {
                StopCoroutine(coroutineDict[objTransform]);
                coroutineDict.Remove(objTransform);
            }
        }

        /// <summary>
        /// Stops a coroutine and resets the object's position.
        /// </summary>
        /// <param name="coroutineDict">The dictionary tracking active coroutines.</param>
        /// <param name="objTransform">The Transform whose coroutine should be stopped.</param>
        /// <param name="pickupGroup">The PickupGroup containing the original positions.</param>
        private void StopAndResetCoroutine(Dictionary<Transform, Coroutine> coroutineDict, Transform objTransform, PickupGroup pickupGroup)
        {
            if (coroutineDict.ContainsKey(objTransform))
            {
                StopCoroutine(coroutineDict[objTransform]);
                coroutineDict.Remove(objTransform);
                objTransform.position = pickupGroup.originalPositions.ContainsKey(objTransform)
                    ? pickupGroup.originalPositions[objTransform]
                    : objTransform.position;
            }
        }

        #endregion
    }
    // Define the missing CollectableOnly attribute
    public class CollectableOnlyAttribute : PropertyAttribute
    {
        // You can add any custom logic or fields here if needed
    }
}
