using UnityEditor;
using UnityEngine;
using ViRSE.PluginRuntime.VComponents;

//[CustomPropertyDrawer(typeof(GeneralInteractionConfig))]
public class PushActivatableConfigDrawer : PropertyDrawer
{
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        float totalHeight = 0;
        var iterator = property.Copy();
        var endProperty = iterator.GetEndProperty();
        iterator.NextVisible(true); // Move to the first child property
        while (!SerializedProperty.EqualContents(iterator, endProperty))
        {
            totalHeight += EditorGUI.GetPropertyHeight(iterator, true) + EditorGUIUtility.standardVerticalSpacing;
            iterator.NextVisible(false);
        }
        return totalHeight;
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        var iterator = property.Copy();
        var endProperty = iterator.GetEndProperty();
        iterator.NextVisible(true); // Move to the first child property
        position.height = EditorGUIUtility.singleLineHeight;
        while (!SerializedProperty.EqualContents(iterator, endProperty))
        {
            DrawPropertyField(position, iterator);
            position.y += EditorGUI.GetPropertyHeight(iterator, true) + EditorGUIUtility.standardVerticalSpacing;
            iterator.NextVisible(false);
        }
    }

    private void DrawPropertyField(Rect position, SerializedProperty property)
    {
        if (property.propertyType == SerializedPropertyType.Generic)
        {
            var iterator = property.Copy();
            var endProperty = iterator.GetEndProperty();
            iterator.NextVisible(true); // Move to the first child property
            while (!SerializedProperty.EqualContents(iterator, endProperty))
            {
                EditorGUI.PropertyField(position, iterator, true);
                position.y += EditorGUI.GetPropertyHeight(iterator, true) + EditorGUIUtility.standardVerticalSpacing;
                iterator.NextVisible(false);
            }
        }
        else
        {
            EditorGUI.PropertyField(position, property, true);
        }
    }
}
