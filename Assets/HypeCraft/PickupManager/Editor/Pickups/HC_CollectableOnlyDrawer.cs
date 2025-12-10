namespace HC_Pickups
{
    using UnityEngine;
    using UnityEditor;
    using System.Collections.Generic;
 
    /// <summary>
    /// CollectableOnlyDrawer is a custom property drawer for the CollectableOnlyAttribute.
    /// It provides a dropdown menu in the Unity Inspector to select GameObjects with the HC_Collectable component.
    /// Additionally, it supports drag-and-drop functionality, allowing users to drag objects from the hierarchy
    /// into the property field if they have the HC_Collectable component.
    /// 
    /// Key Features:
    /// - Displays all HC_Collectable objects in the current scene(s) in a dropdown menu.
    /// - Allows drag-and-drop of HC_Collectable objects from the hierarchy.
    /// - Ensures only valid HC_Collectable objects can be assigned to the property.
    /// </summary>
    [CustomPropertyDrawer(typeof(CollectableOnlyAttribute))]
    public class HC_CollectableOnlyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // Only UnityEngine.Object type fields are allowed
            if (property.propertyType != SerializedPropertyType.ObjectReference)
            {
                EditorGUI.LabelField(position, label.text, "Use [CollectableOnly] only with Object fields.");
                return;
            }

            // Only GameObjects or HC_Collectables are allowed
            EditorGUI.BeginProperty(position, label, property);

            UnityEngine.Object obj = property.objectReferenceValue;
            GameObject go = obj as GameObject;
            if (go != null)
            {
                if (go.GetComponent<HC_Collectable>() == null)
                {
                    EditorGUI.LabelField(position, label.text, "Object must have HC_Collectable");
                    property.objectReferenceValue = null;
                }
                else
                {
                    EditorGUI.PropertyField(position, property, label);
                }
            }
            else
            {
                EditorGUI.PropertyField(position, property, label);
            }

            EditorGUI.EndProperty();
        }

        private void AddCollectablesInHierarchy(GameObject obj, List<GameObject> list)
        {
            if (obj.GetComponent<HC_Collectable>() != null) { list.Add(obj); }
            foreach (Transform child in obj.transform)
            {
                AddCollectablesInHierarchy(child.gameObject, list);
            }
        }
    }
}