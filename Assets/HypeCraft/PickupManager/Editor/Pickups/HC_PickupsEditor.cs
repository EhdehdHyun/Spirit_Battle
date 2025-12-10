namespace HC_Pickups
{
    using System.Collections.Generic;
    using System.Linq;
    using UnityEditor;
    using UnityEngine;

    // This custom editor script provides a custom inspector for the HC_PickupsManager MonoBehaviour.
    // It allows users to add and remove Pickup Managers, view and modify settings for animations, effects, and audio.
    // The script also adds buttons to toggle the visibility of settings for float animations, rotation animations, and effects.
    // Serialized properties are used to manage the fields of HC_PickupsManager, and the inspector is updated accordingly.

    #region HC_PickupsEditor Class
    // Custom editor for the TesPKManager script
    [CustomEditor(typeof(HC_PickupsManager))]
    public class HC_PickupsEditor : Editor
    {
        #region Variables
        // GUI style for customizing the appearance of header labels in the inspector.
        GUIStyle headerStyle;

        // Serialized property for the list of Pickup Managers in HC_PickupsManager.
        SerializedProperty pickupManagers;

        // Serialized property for the audio settings in HC_PickupsManager.
        SerializedProperty audioSettings;

        // Serialized property for the list of Pickup Managers in HC_PickupsManager.
        SerializedProperty pickupManagersProperty;

        // Serialized properties for buttons that toggle visibility of animation and effect settings.
        SerializedProperty floatAnimationButton;
        SerializedProperty rotationAnimationButton;
        SerializedProperty effectSettingsButton;
        SerializedProperty audioSettingsButton;

        // Serialized property for the list of objects to animate in HC_PickupsManager.
        SerializedProperty objectsToAnimate2;

        // Dictionaries to track the visibility state of settings for each Pickup Manager.
        Dictionary<int, bool> showFloatSettings = new Dictionary<int, bool>();
        Dictionary<int, bool> showRotationSettings = new Dictionary<int, bool>();
        Dictionary<int, bool> showEffectSettings = new Dictionary<int, bool>();
        bool showAudioSettings = false;
        Dictionary<int, bool> showObjectsList = new Dictionary<int, bool>();

        Dictionary<string, int> previousObjectListSizes = new Dictionary<string, int>();
        #endregion

        #region Unity Methods
        // Initializes the header style for custom labels in the inspector.
        private void Awake()
        {
            headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                normal = { textColor = Color.white }, // White text color
                fontSize = 14,                        // Font size
                alignment = TextAnchor.MiddleLeft     // Align text to the left
            };
        }

        // Initializes serialized properties and visibility states when the editor is enabled.
        private void OnEnable()
        {
            pickupManagers = serializedObject.FindProperty("pickupManagers");
            audioSettings = serializedObject.FindProperty("audioSettings");

            // Retrieve the Pickup Managers list from SerializedObject
            pickupManagersProperty = serializedObject.FindProperty("pickupManagers");

            floatAnimationButton = serializedObject.FindProperty("floatAnimationButton");
            rotationAnimationButton = serializedObject.FindProperty("rotationAnimationButton");
            effectSettingsButton = serializedObject.FindProperty("effectSettingsButton");
            audioSettingsButton = serializedObject.FindProperty("audioSettingsButton");

            objectsToAnimate2 = serializedObject.FindProperty("objectsToAnimate2");

            showFloatSettings = new Dictionary<int, bool>();
            showRotationSettings = new Dictionary<int, bool>();
            showEffectSettings = new Dictionary<int, bool>();
            showObjectsList = new Dictionary<int, bool>();
        }

        // Draws the custom inspector UI for HC_PickupsManager.
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            HC_PickupsManager pickupsManager = (HC_PickupsManager)target;

            DrawCustomHeader();
            DrawInstructions();

            DrawAddNewPickupManagerButton(pickupsManager);
            EditorGUILayout.Space(1);

            DrawRemoveLastPickupManagerButton(pickupsManager);
            EditorGUILayout.Space(10);

            DrawPickupManagersList();
            EditorGUILayout.Space(10);

            DrawAudioSettingsButton();
            EditorGUILayout.Space(10);

            // Draw warnings for HC_AudioSettings
            DrawAudioWarnings(pickupsManager.audioSettings);

            if (showAudioSettings)
            {
                ShowAudioProperties((HC_AudioSettings)audioSettings.objectReferenceValue);
            }

            if (pickupManagers != null && pickupManagers.isArray)
            {
                for (int i = 0; i < pickupManagers.arraySize; i++)
                {
                    DrawPickupManagerSettings(i);
                }
            }

            serializedObject.ApplyModifiedProperties();
        }
        #endregion

        #region Draw Methods
        // Draws the header section with a custom style.
        private void DrawCustomHeader()
        {
            GUI.color = Color.yellow;
            EditorGUILayout.LabelField("Pickups Manager - Animation, Effects and Audio Control", headerStyle);
            EditorGUILayout.Space(10);
        }

        // Displays instructions for using the HC_PickupsManager editor.
        private void DrawInstructions()
        {
            GUI.color = Color.green;
            EditorGUILayout.LabelField("Tip: Hover your mouse over the text to see instructions.");
            EditorGUILayout.LabelField("Instructions are only displayed when you are not in \"Play Mode\".");
            EditorGUILayout.LabelField("Don't put this code in prefab.");
            GUI.color = Color.white;
            EditorGUILayout.Space(10);
        }

        // Display the pickupManagers list
        // Iterates through the pickupManagersProperty array and displays each Pickup Manager in the inspector.
        private void DrawPickupManagersList()
        {
            for (int i = 0; i < pickupManagersProperty.arraySize; i++)
            {
                SerializedProperty pickupManager = pickupManagersProperty.GetArrayElementAtIndex(i);
                EditorGUILayout.PropertyField(pickupManager, new GUIContent($"Pickup Manager {i + 1}"), true);
            }
        }

        // Add "Add New Pickup Manager" button
        // Displays a button in the inspector to add a new Pickup Manager to the list.
        private void DrawAddNewPickupManagerButton(HC_PickupsManager pickupsManager)
        {
            if (GUILayout.Button(new GUIContent("Add New Pickup Manager", "Adds a new Pickup Manager class and creates adjustable settings for it"),
                CreateButtonStyle(Color.gray, Color.black, 2, "", ""), GUILayout.Height(25f), GUILayout.Width(240f)))
            {
                pickupsManager.AddNewPickupManager();
            }
        }

        // Draws a button to remove the last Pickup Manager from the list
        // Displays a button in the inspector to remove the last Pickup Manager from the list.
        // If the list is empty, a warning message is displayed.
        private void DrawRemoveLastPickupManagerButton(HC_PickupsManager pickupsManager)
        {
            if (GUILayout.Button(new GUIContent("Remove Last Pickup Manager", "Removes the last Pickup Manager from the list"),
                CreateButtonStyle(Color.black, Color.black, 2, "", ""), GUILayout.Height(25f), GUILayout.Width(240f)))
            {
                if (pickupManagersProperty.arraySize > 0)
                {
                    pickupsManager.RemoveLastPickupManager();
                }
            }

            // Display the error message if the list is empty
            if (pickupManagersProperty.arraySize == 0)
            {
                EditorGUILayout.Space(10);
                EditorGUILayout.HelpBox("The Pickup Managers list is empty. Please add at least one Pickup Manager!", MessageType.Warning);
            }
        }

        // Draws a button to toggle the visibility of audio settings
        // Displays a button in the inspector to show or hide the audio settings for collectibles.
        private void DrawAudioSettingsButton()
        {
            EditorGUILayout.LabelField("The button shows different settings, Click the button.");

            if (GUILayout.Button(new GUIContent("Audio Settings", "Show the sound settings for collectibles"),
                CreateButtonStyle(Color.red, Color.black, 2, "", ""),
                GUILayout.Height(25f), GUILayout.Width(200f)))
            {
                showAudioSettings = !showAudioSettings;
            }
        }

        // Displays warnings for missing or invalid audio settings
        // Checks the audio settings and displays appropriate warnings if they are missing or invalid.
        private void DrawAudioWarnings(HC_AudioSettings audioSettings)
        {
            if (audioSettings == null)
            {
                EditorGUILayout.HelpBox("HC_AudioSettings component is missing. Please add it to the GameObject.", MessageType.Error);
                return;
            }

            // Check if audioEventsList is null or empty
            if (audioSettings.audioEventsList == null || audioSettings.audioEventsList.Count == 0)
            {
                EditorGUILayout.HelpBox("Audio Events List is empty! Please add audio events.", MessageType.Warning);
            }
            else
            {
                // Check for null elements in the audioEventsList
                bool hasNullElements = audioSettings.audioEventsList.Any(eventItem => eventItem == null);
                if (hasNullElements)
                {
                    EditorGUILayout.HelpBox("The Audio Events List contains empty elements. Please remove or replace them.", MessageType.Error);
                }
            }
        }

        // Draws the settings for a specific Pickup Manager, including animations and effects
        // Displays the settings for a specific Pickup Manager in the inspector, including float, rotation, and effect settings.
        private void DrawPickupManagerSettings(int index)
        {
            SerializedProperty pickupManager = pickupManagers.GetArrayElementAtIndex(index);
            SerializedProperty objectsList = pickupManager.FindPropertyRelative("objectsList");
            SerializedProperty headerText = pickupManager.FindPropertyRelative("headerText");
            SerializedProperty floatAnimation = pickupManager.FindPropertyRelative("floatAnimation");
            SerializedProperty rotationAnimation = pickupManager.FindPropertyRelative("rotationAnimation");
            SerializedProperty effectSettings = pickupManager.FindPropertyRelative("effectSettings");

            EditorGUILayout.Space(10);
            GUI.color = Color.cyan;
            ShowHeaderText(headerText, index);
            GUI.color = Color.white;
            EditorGUILayout.Space(5);

            // Initialize visibility states if not already set
            if (!showObjectsList.ContainsKey(index)) showObjectsList[index] = false;
            if (!showFloatSettings.ContainsKey(index)) showFloatSettings[index] = false;
            if (!showRotationSettings.ContainsKey(index)) showRotationSettings[index] = false;
            if (!showEffectSettings.ContainsKey(index)) showEffectSettings[index] = false;

            FixDuplicatesInList(objectsList);

            // Show objects list
            ShowObjectsList(objectsList);
            EditorGUILayout.Space(5f);

            EditorGUILayout.LabelField("The buttons show different settings, Click the buttons.");

            // Draw buttons for Float, Rotation, and Effect settings
            DrawFloatSettingsButton(index, floatAnimation);
            DrawRotationSettingsButton(index, rotationAnimation);
            DrawEffectSettingsButton(index, effectSettings);

            HC_PickupsManager pickupsManager = (HC_PickupsManager)target;
            EditorGUILayout.Space();

            // Always show error if Effect Prefab List is missing for this Pickup Manager
            if (effectSettings != null && effectSettings.objectReferenceValue != null)
            {
                var effectSettingsSO = effectSettings.objectReferenceValue as HC_EffectSettingsSO;
                if (effectSettingsSO != null && effectSettingsSO.effectPrefabList == null)
                {
                    EditorGUILayout.HelpBox(
                        "Effect Prefab List is not assigned. Please assign an HC_EffectListSO ScriptableObject.",
                        MessageType.Error
                    );
                }
            }
        }

        private void DrawFloatSettingsButton(int index, SerializedProperty floatAnimation)
        {
            // Define button dimensions and store the original background color.
            float buttonHeight = 25f;
            float buttonWidth = 200f;
            Color originalBackgroundColor = GUI.backgroundColor;

            // Draw the button to toggle the visibility of float animation settings.
            if (GUILayout.Button(new GUIContent("Float Settings", "Show the float animation settings"),
                CreateButtonStyle(Color.green, Color.black, 2, "Float Settings", "Hovering over Float Settings"),
                GUILayout.Height(buttonHeight), GUILayout.Width(buttonWidth)))
            {
                // Toggle the visibility state for the float animation settings of the current Pickup Manager.
                showFloatSettings[index] = !showFloatSettings[index];
            }

            // If the float animation settings are visible, display them in the inspector.
            if (showFloatSettings[index])
            {
                // Change the background color to indicate the active state.
                GUI.backgroundColor = Color.green;

                // Check if the float animation property is valid and has a reference value.
                if (floatAnimation != null && floatAnimation.objectReferenceValue != null)
                {
                    // Display the float animation properties in the inspector.
                    ShowFloatProperties(floatAnimation);
                }
                else
                {
                    // Display a warning if the float animation component is missing.
                    EditorGUILayout.HelpBox("Float Animation component is missing!", MessageType.Warning);
                }
            }

            // Restore the original background color.
            GUI.backgroundColor = originalBackgroundColor;

            // Add a small space after the button for better layout.
            EditorGUILayout.Space(0.5f);
        }

        private void DrawRotationSettingsButton(int index, SerializedProperty rotationAnimation)
        {
            // Define button dimensions and store the original background color for later restoration.
            float buttonHeight = 25f;
            float buttonWidth = 200f;
            Color originalBackgroundColor = GUI.backgroundColor;

            // Draw the button to toggle the visibility of rotation animation settings.
            if (GUILayout.Button(new GUIContent("Rotation Settings", "Shows the rotation animation settings"),
               CreateButtonStyle(Color.blue, Color.black, 2, "Rotation Settings", "Hovering over Rotation Settings"),
               GUILayout.Height(buttonHeight), GUILayout.Width(buttonWidth)))
            {
                // Toggle the visibility state for the rotation animation settings of the current Pickup Manager.
                showRotationSettings[index] = !showRotationSettings[index];
            }

            // If the rotation animation settings are visible, display them in the inspector.
            if (showRotationSettings[index])
            {
                // Change the background color to indicate the active state.
                GUI.backgroundColor = Color.cyan;

                // Check if the rotation animation property is valid and has a reference value.
                if (rotationAnimation != null && rotationAnimation.objectReferenceValue != null)
                {
                    // Display the rotation animation properties in the inspector.
                    ShowRotationProperties(rotationAnimation);
                }
                else
                {
                    // Display a warning if the rotation animation component is missing.
                    EditorGUILayout.HelpBox("Rotation Animation component is missing!", MessageType.Warning);
                }
            }

            // Restore the original background color to maintain UI consistency.
            GUI.backgroundColor = originalBackgroundColor;

            // Add a small space after the button for better layout.
            EditorGUILayout.Space(0.5f);
        }

        // In DrawEffectSettingsButton, add the error message when the Effect Settings are expanded

        private void DrawEffectSettingsButton(int index, SerializedProperty effectSettings)
        {
            float buttonHeight = 25f;
            float buttonWidth = 200f;
            Color originalBackgroundColor = GUI.backgroundColor;

            if (GUILayout.Button(new GUIContent("Effect Settings", "Show the effect settings" +
                "\nNote: Effects are placed at the origin of the object."),
                CreateButtonStyle(Color.yellow, Color.black, 2, "Effect Settings", "Hovering over Effect Settings"),
                GUILayout.Height(buttonHeight), GUILayout.Width(buttonWidth)))
            {
                showEffectSettings[index] = !showEffectSettings[index];
            }

            if (showEffectSettings[index])
            {
                GUI.backgroundColor = Color.yellow;
                GUILayout.Space(5f);

                // Show error if Effect Prefab List is missing
                if (effectSettings != null && effectSettings.objectReferenceValue != null)
                {
                    var effectSettingsSO = effectSettings.objectReferenceValue as HC_EffectSettingsSO;
                    if (effectSettingsSO != null && effectSettingsSO.effectPrefabList == null)
                    {
                        EditorGUILayout.HelpBox(
                            "Effect Prefab List is not assigned. Please assign an HC_EffectListSO ScriptableObject.",
                            MessageType.Error
                        );
                    }
                }

                if (effectSettings != null && effectSettings.objectReferenceValue != null)
                {
                    EditorGUILayout.Space(5f);
                    EditorGUILayout.LabelField("Effect Settings", EditorStyles.boldLabel);

                    ShowEffectProperties(effectSettings);
                }
                else
                {
                    EditorGUILayout.HelpBox("Effect Settings component is missing!", MessageType.Warning);
                }

                DrawEffectSettingsValidation((HC_PickupsManager)target);
            }

            GUI.backgroundColor = originalBackgroundColor;
            EditorGUILayout.Space(0.5f);
        }


        // Show header text for each Pickup Manager
        private void ShowHeaderText(SerializedProperty headerText, int luku)
        {
            GUI.backgroundColor = Color.cyan;
            luku++;

            EditorGUILayout.PropertyField(headerText, new GUIContent("Group " + luku), true);
            serializedObject.ApplyModifiedProperties();
        }

        // Show objects list for each Pickup Manager
        private void ShowObjectsList(SerializedProperty objects)
        {
            GUI.backgroundColor = Color.gray;

            EditorGUILayout.PropertyField(objects, new GUIContent("Objects List"), true);
            serializedObject.ApplyModifiedProperties();
        }

        // Show properties for Float Animation
        private void ShowFloatProperties(SerializedProperty floatAnimation)
        {
            if (floatAnimation != null && floatAnimation.objectReferenceValue != null)
            {
                SerializedObject floatSerializedObject = new SerializedObject(floatAnimation.objectReferenceValue);
                floatSerializedObject.Update();

                SerializedProperty floatEnabled = floatSerializedObject.FindProperty("floatEnabled");
                SerializedProperty floatCurve = floatSerializedObject.FindProperty("floatCurve");
                SerializedProperty floatBaseHeight = floatSerializedObject.FindProperty("floatBaseHeight");
                SerializedProperty floatMovementSpeed = floatSerializedObject.FindProperty("floatMovementSpeed");
                SerializedProperty floatDistance = floatSerializedObject.FindProperty("floatDistance");

                EditorGUILayout.PropertyField(floatEnabled, new GUIContent("On/Off"), true);
                EditorGUILayout.PropertyField(floatCurve, new GUIContent("Animation Curve"), true);
                EditorGUILayout.PropertyField(floatBaseHeight, new GUIContent("Base Height"), true);
                EditorGUILayout.PropertyField(floatMovementSpeed, new GUIContent("Movement Speed"), true);
                EditorGUILayout.PropertyField(floatDistance, new GUIContent("Movement Distance"), true);
                EditorGUILayout.Space();

                floatSerializedObject.ApplyModifiedProperties();
            }
            else
            {
                EditorGUILayout.HelpBox("Float Animation component is missing!", MessageType.Warning);
            }
        }

        private void ShowRotationProperties(SerializedProperty rotationAnimation)
        {
            if (rotationAnimation != null && rotationAnimation.objectReferenceValue != null)
            {
                // Create a SerializedObject to access the properties of the rotation animation ScriptableObject.
                SerializedObject rotationSerializedObject = new SerializedObject(rotationAnimation.objectReferenceValue);
                rotationSerializedObject.Update();

                // Retrieve the properties of the rotation animation settings.
                SerializedProperty rotationEnabled = rotationSerializedObject.FindProperty("rotationEnabled");
                SerializedProperty rotationCurve = rotationSerializedObject.FindProperty("rotationCurve");
                SerializedProperty rotationAxis = rotationSerializedObject.FindProperty("rotationAxis");
                SerializedProperty rotationMovementSpeed = rotationSerializedObject.FindProperty("rotationMovementSpeed");

                // Display the properties in the inspector with appropriate labels.
                EditorGUILayout.PropertyField(rotationEnabled, new GUIContent("On/Off"), true);
                EditorGUILayout.PropertyField(rotationCurve, new GUIContent("Animation Curve"), true);
                EditorGUILayout.PropertyField(rotationAxis, new GUIContent("Rotation Axis"));
                EditorGUILayout.PropertyField(rotationMovementSpeed, new GUIContent("Movement Speed"), true);
                EditorGUILayout.Space();

                // Apply any changes made in the inspector to the SerializedObject.
                rotationSerializedObject.ApplyModifiedProperties();
            }
            else
            {
                // Display a warning if the rotation animation component is missing.
                EditorGUILayout.HelpBox("Rotation Animation component is missing!", MessageType.Warning);
            }
        }

        private void ShowEffectProperties(SerializedProperty effectSettings)
        {

            if (effectSettings != null && effectSettings.objectReferenceValue != null)
            {
                // Create a SerializedObject to access the properties of the effect settings ScriptableObject.
                SerializedObject effectSerializedObject = new SerializedObject(effectSettings.objectReferenceValue);
                effectSerializedObject.Update();

                // Retrieve the properties of the effect settings.
                SerializedProperty effectEnabled = effectSerializedObject.FindProperty("effectEnabled");
                SerializedProperty effectIndex = effectSerializedObject.FindProperty("effectIndex");
                SerializedProperty effectPrefabList = effectSerializedObject.FindProperty("effectPrefabList");
                SerializedProperty inspectorEffectPrefabs = effectSerializedObject.FindProperty("inspectorEffectPrefabs");

                // Display the properties in the inspector with appropriate labels.
                EditorGUILayout.PropertyField(effectEnabled, new GUIContent("On/Off"));
                EditorGUILayout.PropertyField(effectIndex, new GUIContent("Effect Index"));
                EditorGUILayout.PropertyField(effectPrefabList, new GUIContent("Effect Prefab List"));
                EditorGUILayout.PropertyField(inspectorEffectPrefabs, new GUIContent("Inspector Effect Prefabs"), true);
                EditorGUILayout.Space();

                // Apply any changes made in the inspector to the SerializedObject.
                effectSerializedObject.ApplyModifiedProperties();
            }
            else
            {
                // Display a warning if the effect settings component is missing.
                EditorGUILayout.HelpBox("Effect Settings component is missing!", MessageType.Warning);
            }
        }

        // Displays the properties of the audio settings in the inspector.
        // This method is called when the audio settings are visible.
        private void ShowAudioProperties(HC_AudioSettings audioManager)
        {
            if (audioManager == null)
            {
                // Display an error if the audio settings are not assigned.
                EditorGUILayout.HelpBox("Audio Settings Collection is not assigned.", MessageType.Error);
                return;
            }

            // Create a SerializedObject to access the properties of the audio settings.
            SerializedObject audioSerializedObject = new SerializedObject(audioManager);
            audioSerializedObject.Update();

            // Retrieve the properties of the audio settings.
            SerializedProperty audioEnabled = audioSerializedObject.FindProperty("audioEnabled");
            SerializedProperty audioEventIndex = audioSerializedObject.FindProperty("audioEventIndex");
            SerializedProperty audioEventsList = audioSerializedObject.FindProperty("audioEventsList");
            SerializedProperty moreSettings = audioSerializedObject.FindProperty("moreSettings");

            // Display the properties in the inspector with appropriate labels.
            if (audioEnabled != null) EditorGUILayout.PropertyField(audioEnabled);
            if (audioEventIndex != null) EditorGUILayout.PropertyField(audioEventIndex);
            if (audioEventsList != null) EditorGUILayout.PropertyField(audioEventsList);
            if (moreSettings != null) EditorGUILayout.PropertyField(moreSettings);

            // If "More Settings" is enabled, display additional settings for each audio event.
            if (moreSettings != null && moreSettings.boolValue)
            {
                EditorGUILayout.Space(4); // Add space after "More Settings"
                for (int i = 0; i < audioEventsList.arraySize; i++)
                {
                    SerializedProperty audioEvent = audioEventsList.GetArrayElementAtIndex(i);
                    if (audioEvent != null && audioEvent.objectReferenceValue != null)
                    {
                        // Create a SerializedObject for each audio event to access its properties.
                        SerializedObject audioEventSerializedObject = new SerializedObject(audioEvent.objectReferenceValue);
                        audioEventSerializedObject.Update();

                        // Retrieve the properties of the audio event.
                        SerializedProperty audioClips = audioEventSerializedObject.FindProperty("audioClips");
                        SerializedProperty volume = audioEventSerializedObject.FindProperty("volume");
                        SerializedProperty randomizeVolume = audioEventSerializedObject.FindProperty("randomizeVolume");
                        SerializedProperty pitch = audioEventSerializedObject.FindProperty("pitch");
                        SerializedProperty randomizePitch = audioEventSerializedObject.FindProperty("randomizePitch");

                        // Add a header for each audio event's settings.
                        EditorGUILayout.Space(20); // Add space above each header.
                        EditorGUILayout.LabelField($"Audio Event {i + 1}: {audioEvent.objectReferenceValue.name}", EditorStyles.boldLabel);

                        // Display the properties of the audio event in the inspector.
                        if (audioClips != null) EditorGUILayout.PropertyField(audioClips, new GUIContent("Audio Clips"), true);
                        if (volume != null) EditorGUILayout.PropertyField(volume, new GUIContent("Volume"));
                        if (randomizeVolume != null) EditorGUILayout.PropertyField(randomizeVolume, new GUIContent("Randomize Volume"));
                        if (pitch != null) EditorGUILayout.PropertyField(pitch, new GUIContent("Pitch"));
                        if (randomizePitch != null) EditorGUILayout.PropertyField(randomizePitch, new GUIContent("Randomize Pitch"));

                        // Apply any changes made in the inspector to the SerializedObject.
                        audioEventSerializedObject.ApplyModifiedProperties();
                    }
                }
            }

            // Add a button to play the selected audio event from the editor.
            AddPlayAudioEventButton((HC_PickupsManager)target, 20f);
            audioSerializedObject.ApplyModifiedProperties();
        }
        // Utility function to create a custom button style
        private GUIStyle CreateButtonStyle(Color backgroundColor, Color borderColor, int borderWidth, string normalText, string hoverText)
        {
            GUIStyle buttonStyle = new GUIStyle(GUI.skin.button)
            {
                normal = { textColor = Color.white, background = CreateButtonBackgroundWithBorder(backgroundColor, borderColor, borderWidth) },
                hover = { textColor = Color.cyan, background = CreateButtonBackgroundWithBorder(backgroundColor, borderColor, borderWidth) }, // Cyan text for hover state
                fontStyle = FontStyle.BoldAndItalic, // Bold font
                fontSize = 14, // Font size
                padding = new RectOffset(10, 10, 2, 2) // Padding for button text
            };

            buttonStyle.onHover.textColor = Color.cyan; // Change text color on hover
            buttonStyle.onHover.background = CreateButtonBackgroundWithBorder(backgroundColor, borderColor, borderWidth); // Change background on hover

            return buttonStyle;
        }

        private Texture2D CreateButtonBackgroundWithBorder(Color backgroundColor, Color borderColor, int borderWidth)
        {
            int width = 10;
            int height = 25;
            Texture2D texture = new Texture2D(width, height);

            // Fill the texture with the background color
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (x < borderWidth || x >= width - borderWidth || y < borderWidth || y >= height - borderWidth)
                    {
                        texture.SetPixel(x, y, borderColor); // Set border color
                    }
                    else
                    {
                        texture.SetPixel(x, y, backgroundColor); // Set background color
                    }
                }
            }
            texture.Apply();
            return texture;
        }

        private void AddPlayAudioEventButton(HC_PickupsManager pickupsManager, float buttonHeight)
        {
            EditorGUILayout.Space(10); // Add space before the "Play Audio" button.
            if (GUILayout.Button(new GUIContent(
            "Play Audio",
            "Update the button status by pressing the editor \"Play\" if the sounds are not playing"),
        CreateButtonStyle(Color.red, Color.black, 2, "Play Audio Event", "Hovering over Play Audio Event"),
        GUILayout.Height(buttonHeight), GUILayout.Width(150f)))
            {
                // Check if audio settings and audio events are available.
                if (pickupsManager.audioSettings != null && pickupsManager.audioSettings.audioEventsList.Count > 0)
                {
                    // Clamp the selected audio event index to ensure it is within bounds.
                    int clampedIndex = Mathf.Clamp(pickupsManager.audioSettings.audioEventIndex, 0, pickupsManager.audioSettings.audioEventsList.Count - 1);
                    HC_AudioEvent selectedAudioEvent = pickupsManager.audioSettings.audioEventsList[clampedIndex];
                    if (selectedAudioEvent != null)
                    {
                        // Play the selected audio event from the editor.
                        selectedAudioEvent.PlayFromEditor();
                    }
                    else
                    {
                        Debug.LogWarning("Selected HC_Audio Event is null!");
                    }
                }
                else
                {
                    Debug.LogWarning("No audio events available to play!");
                }
            }
        }

        #region Utility Methods
        void FixDuplicatesInList(SerializedProperty list)
        {
            // We need to use a unique identifier to find the previous size of the correct list.
            int previousSize = -1;
            string key = list.serializedObject.targetObject.GetInstanceID() + ":" + list.propertyPath;
            if (previousObjectListSizes.ContainsKey(key))
            {
                previousSize = previousObjectListSizes[key];
            }

            // Handle cases where the user adds new elements to the object list.
            if (previousSize >= 0 && list.arraySize >= 2 && list.arraySize > previousSize)
            {
                var lastElement = list.GetArrayElementAtIndex(list.arraySize - 1);
                var prevElement = list.GetArrayElementAtIndex(list.arraySize - 2);

                // The new items will show "None". Normally Unity lists in inspector would duplicate the previous value.
                if (lastElement != null && lastElement.objectReferenceValue == prevElement.objectReferenceValue)
                {
                    lastElement.objectReferenceValue = null;
                }
            }

            // Handle case where user selects an object from the popup list.
            var seenObjects = new HashSet<GameObject>();
            for (int i = 0; i < list.arraySize; i++)
            {
                SerializedProperty element = list.GetArrayElementAtIndex(i);
                var obj = element.objectReferenceValue as GameObject;
                if (obj != null)
                {
                    if (seenObjects.Contains(obj))
                    {
                        // Do not allow multiple same. Remove the duplicate.
                        list.DeleteArrayElementAtIndex(i);
                    }
                    else
                    {
                        seenObjects.Add(obj);
                    }
                }
            }

            previousObjectListSizes[key] = list.arraySize;
        }
        /// <summary>
        /// Validates the effect settings for each PickupGroup in the manager and displays warnings/errors in the Inspector.
        /// </summary>
        private void DrawEffectSettingsValidation(HC_PickupsManager manager)
        {
            if (manager == null || manager.pickupManagers == null)
                return;

            for (int i = 0; i < manager.pickupManagers.Count; i++)
            {
                var group = manager.pickupManagers[i];
                var effectSettings = group.effectSettings;
                if (effectSettings != null)
                {
                    var effectListSO = effectSettings.effectPrefabList;

                    if (effectListSO.effectList == null || effectListSO.effectList.Count == 0)
                    {
                        EditorGUILayout.HelpBox(
                            "Effect List is empty! Please add at least one effect prefab.",
                            MessageType.Error
                        );
                    }
                    else
                    {
                        for (int j = 0; j < effectListSO.effectList.Count; j++)
                        {
                            if (effectListSO.effectList[j] == null)
                            {
                                EditorGUILayout.HelpBox(
                                    $"PickupGroup '{group.headerText}': Effect List element at index {j} is empty (None). Please assign a GameObject.",
                                    MessageType.Error
                                );
                            }
                        }
                    }

                }
            }
        }

        #endregion
    }
    #endregion
    #endregion
}