namespace HC_Pickups
{
    using UnityEngine;
    using UnityEditor;

    // Custom editor for HC_AudioEvent ScriptableObject.
    // Adds buttons to play and stop audio directly from the Unity editor.

    [CustomEditor(typeof(HC_AudioEvent))]
    public class HC_AudioEventEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            // Overrides the default inspector GUI to add custom buttons for playing and stopping audio.
            EditorGUILayout.LabelField("Scriptableobject audio", EditorStyles.boldLabel);

            // Reference to the HC_AudioEvent being edited in the inspector.
            HC_AudioEvent audioEvent = (HC_AudioEvent)target;

            // Button to play the audio event directly from the editor.
            if (GUILayout.Button("Play"))
            {
                audioEvent.PlayFromEditor();
            }

            // Button to stop the audio event directly from the editor.
            if (GUILayout.Button("Stop"))
            {
                audioEvent.StopFromEditor();
            }

            // Draw the default inspector fields for the HC_AudioEvent.
            DrawDefaultInspector();
        }
    }
}
