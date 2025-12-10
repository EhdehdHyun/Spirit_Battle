namespace HC_Pickups
{
    using UnityEditor;

    [CustomEditor(typeof(HC_EffectListSO))]
    public class HC_EffectListSOEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            // Draw the default inspector
            DrawDefaultInspector();

            var effectListSO = (HC_EffectListSO)target;

            // Show error if the list is null or empty
            if (effectListSO.effectList == null || effectListSO.effectList.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    "Effect List is empty! Please add at least one effect prefab.",
                    MessageType.Error
                );
            }
            else
            {
                for (int i = 0; i < effectListSO.effectList.Count; i++)
                {
                    if (effectListSO.effectList[i] == null)
                    {
                        EditorGUILayout.HelpBox(
                            $"Effect List element at index {i} is empty (None). Please assign a GameObject.",
                            MessageType.Error
                        );
                    }
                }
            }
        }
    }
}
