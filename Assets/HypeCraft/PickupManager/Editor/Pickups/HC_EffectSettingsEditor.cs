namespace HC_Pickups
{
    using UnityEditor;

    // Custom editor for HC_EffectSettingsSO ScriptableObject.
    // Displays an error if the Effect Prefab List is missing and allows editing other properties.

    [CustomEditor(typeof(HC_EffectSettingsSO))]
    public class HC_EffectSettingsEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            // Update the serialized object to reflect the current state.
            serializedObject.Update();

            // Reference to the HC_EffectSettingsSO instance being edited.
            HC_EffectSettingsSO effectSettings = (HC_EffectSettingsSO)target;

            // Draw the default inspector fields for HC_EffectSettingsSO.
            DrawDefaultInspector();

            // Apply any changes made in the inspector to the serialized object.
            serializedObject.ApplyModifiedProperties();
        }
    }
}
