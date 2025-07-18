#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;
using System;
using System.Reflection;

namespace VE2.Common.Shared
{
    [CustomPropertyDrawer(typeof(InterfaceReference<>), true)]
    internal class InterfaceComponentReferenceDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            SerializedProperty gameObjectProp = property.FindPropertyRelative("_gameObject");

            // Get the interface type T
            Type interfaceType = GetInterfaceTypeFromProperty(property);

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

        private Type GetInterfaceTypeFromProperty(SerializedProperty property)
        {
            // Try to get the target object of the serialized property
            object targetObject = property.serializedObject.targetObject;

            // Use reflection to get the field from the serialized object
            string[] fieldPath = property.propertyPath.Replace(".Array.data[", "[").Split('.');
            object currentObject = targetObject;

            for (int i = 0; i < fieldPath.Length; i++)
            {
                string fieldName = fieldPath[i];

                if (fieldName.Contains("["))
                {
                    int start = fieldName.IndexOf("[") + 1;
                    int end = fieldName.IndexOf("]");
                    int index = int.Parse(fieldName.Substring(start, end - start));
                    string arrayFieldName = fieldName.Substring(0, fieldName.IndexOf("["));

                    FieldInfo listField = currentObject.GetType().GetField(arrayFieldName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                    var list = listField?.GetValue(currentObject) as System.Collections.IList;
                    currentObject = list?[index];
                }
                else
                {
                    FieldInfo field = currentObject.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                    currentObject = field?.GetValue(currentObject);
                }

                if (currentObject == null)
                    return null;
            }

            Type referenceType = currentObject.GetType();
            return referenceType.IsGenericType ? referenceType.GetGenericArguments()[0] : null;
        }


        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            SerializedProperty gameObjectProp = property.FindPropertyRelative("_gameObject");
            GameObject go = gameObjectProp.objectReferenceValue as GameObject;

            Type interfaceType = GetInterfaceTypeFromProperty(property);

            if (go != null && interfaceType != null && go.GetComponent(interfaceType) == null) //ArgumentException: GetComponent requires that the requested component 'InterfaceReference`1' derives from MonoBehaviour or Component or is an interface.
                return EditorGUIUtility.singleLineHeight * 2.5f;

            return EditorGUIUtility.singleLineHeight;
        }
    }
}
#endif
