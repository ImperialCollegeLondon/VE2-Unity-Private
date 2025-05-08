using UnityEditor;
using UnityEngine;
using System;

namespace VE2.Common.Shared
{
    [CustomPropertyDrawer(typeof(InterfaceReference<>), true)]
    internal class InterfaceComponentReferenceDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            SerializedProperty gameObjectProp = property.FindPropertyRelative("_gameObject");

            // Get the interface type T
            Type referenceType = fieldInfo.FieldType;
            Type interfaceType = referenceType.IsGenericType ? referenceType.GetGenericArguments()[0] : null;

            // Begin property block
            EditorGUI.BeginProperty(position, label, property);

            // Reserve space for the help box if needed
            float lineHeight = EditorGUIUtility.singleLineHeight;
            Rect objectFieldRect = new Rect(position.x, position.y, position.width, lineHeight);

            GameObject assignedGO = gameObjectProp.objectReferenceValue as GameObject;

            EditorGUI.BeginChangeCheck();
            GameObject newGO = EditorGUI.ObjectField(objectFieldRect, label, assignedGO, typeof(GameObject), true) as GameObject;

            if (EditorGUI.EndChangeCheck())
            {
                // Validate on assign
                if (newGO == null || (interfaceType != null && newGO.GetComponent(interfaceType) != null))
                {
                    gameObjectProp.objectReferenceValue = newGO;
                }
                else
                {
                    // Display an error dialog
                    if (newGO != null && interfaceType != null)
                    {
                        EditorUtility.DisplayDialog(
                            "Invalid Component",
                            $"GameObject '{newGO.name}' does not contain a component implementing {interfaceType.Name}. Please assign a valid GameObject.",
                            "Ok"
                        );
                    }
                    gameObjectProp.objectReferenceValue = null;
                }
            }

            EditorGUI.EndProperty();

            // Validation help box
            if (assignedGO != null && interfaceType != null && assignedGO.GetComponent(interfaceType) == null)
            {
                Rect helpBoxRect = new Rect(position.x, position.y + lineHeight + 2, position.width, lineHeight * 1.5f);
                EditorGUI.HelpBox(helpBoxRect, $"Missing component implementing {interfaceType.Name}", MessageType.Error);
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            SerializedProperty gameObjectProp = property.FindPropertyRelative("_gameObject");
            GameObject go = gameObjectProp.objectReferenceValue as GameObject;

            Type referenceType = fieldInfo.FieldType;
            Type interfaceType = referenceType.IsGenericType ? referenceType.GetGenericArguments()[0] : null;

            if (go != null && interfaceType != null && go.GetComponent(interfaceType) == null)
                return EditorGUIUtility.singleLineHeight * 2.5f;

            return EditorGUIUtility.singleLineHeight;
        }
    }
}
