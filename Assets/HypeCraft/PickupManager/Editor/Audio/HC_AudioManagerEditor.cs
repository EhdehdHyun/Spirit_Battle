namespace HC_Pickups
{
    using UnityEngine;
    using UnityEditor;
    using System.Linq;

    // Custom editor for HC_AudioSettings ScriptableObject.
    // Allows modifying audio event properties, playing audio events, and toggling additional settings.

    [CustomEditor(typeof(HC_AudioSettings))]
    public class HC_AudioManagerEditor : Editor
    {
        // Serialized property for the index of the selected audio event in the list.
        SerializedProperty audioEventIndex;

        // Serialized property for the list of audio events in HC_AudioSettings.
        SerializedProperty audioEvents;

        // Serialized property for toggling additional settings in HC_AudioSettings.
        SerializedProperty moreSettings;

        // Tracks whether the audioEventsList in HC_AudioSettings is empty.
        bool isListEmpty = false;

        private void OnEnable()
        {
            // Initializes serialized properties and checks if the audioEventsList is empty.
            audioEventIndex = serializedObject.FindProperty("audioEventIndex");
            audioEvents = serializedObject.FindProperty("audioEventsList");
            moreSettings = serializedObject.FindProperty("moreSettings");

            // Check if the audioEventsList is empty.
            HC_AudioSettings audioSettings = (HC_AudioSettings)target;
            isListEmpty = audioSettings.audioEventsList == null || audioSettings.audioEventsList.Count == 0;
        }

        public override void OnInspectorGUI()
        {
            // Customizes the inspector GUI for HC_AudioSettings.
            // Displays warnings for empty or invalid audioEventsList and allows editing selected audio event properties.

            // Update the serialized object.
            serializedObject.Update();

            HC_AudioSettings audioSettings = (HC_AudioSettings)target;

            // Display a warning if the audioEventsList is empty.
            if (audioSettings.audioEventsList == null || audioSettings.audioEventsList.Count == 0)
            {
                EditorGUILayout.HelpBox("AudioEventsList is empty! Please add an HC_AudioEventSO ScriptableObjects.", MessageType.Warning);
                Debug.LogWarning("AudioEventsList is empty! Please add audio events.");
            }
            else
            {
                // Check if the audioEventsList contains any null elements.
                bool hasNullElements = audioSettings.audioEventsList.Any(eventItem => eventItem == null);
                if (hasNullElements)
                {
                    EditorGUILayout.HelpBox("The AudioEventsList contains empty elements. Please remove or replace them.", MessageType.Error);
                    Debug.LogError("The audioEventsList contains empty elements. Please remove or replace them.");
                }
            }

            // Draw default inspector.
            DrawDefaultInspector();

            // Display properties of the selected audio event if the list is not empty.
            if (audioSettings.audioEventsList != null && audioSettings.audioEventsList.Count > 0)
            {
                int clampIndex = Mathf.Clamp(audioSettings.audioEventIndex, 0, audioSettings.audioEventsList.Count - 1);
                HC_AudioEvent selectedAudioEvent = audioSettings.audioEventsList[clampIndex];

                // Show variables of the selected HC_AudioEvent.
                EditorGUILayout.LabelField("Selected Audio Event Properties", EditorStyles.boldLabel);

                SerializedObject audioEventSerializedObject = new SerializedObject(selectedAudioEvent);
                audioEventSerializedObject.Update();

                EditorGUILayout.PropertyField(audioEventSerializedObject.FindProperty("audioClips"), true);
                EditorGUILayout.PropertyField(audioEventSerializedObject.FindProperty("volume"));
                EditorGUILayout.PropertyField(audioEventSerializedObject.FindProperty("randomizeVolume"));
                EditorGUILayout.PropertyField(audioEventSerializedObject.FindProperty("pitch"));
                EditorGUILayout.PropertyField(audioEventSerializedObject.FindProperty("randomizePitch"));

                audioEventSerializedObject.ApplyModifiedProperties();

                // Only show the "Play Audio Event" button if the target is not HC_AudioSettings.
                if (!(target is HC_AudioSettings))
                {
                    if (GUILayout.Button("Play Audio Event", GUILayout.Width(200f), GUILayout.Height(40f)))
                    {
                        HC_AudioPlayback audioPlayback = FindObjectOfType<HC_AudioPlayback>();
                        audioPlayback.PlayAudioEvent(selectedAudioEvent);
                    }
                }
            }
            else
            {
                EditorGUILayout.HelpBox("No Audio Events in the list. Add HC_AudioEvent ScriptableObjects here!", MessageType.Warning);
            }

            // Apply the modified properties.
            serializedObject.ApplyModifiedProperties();
        }
    }
}
