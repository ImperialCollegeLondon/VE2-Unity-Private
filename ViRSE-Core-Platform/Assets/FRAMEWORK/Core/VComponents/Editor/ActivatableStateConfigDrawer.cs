//using UnityEditor;
//using UnityEngine;
//using ViRSE.PluginRuntime.VComponents;

//[CustomPropertyDrawer(typeof(ActivatableStateConfig))]
//public class ActivatableStateConfigDrawer : PropertyDrawer
//{
//    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
//    {
//        // Calculate the height needed for all the fields
//        float totalHeight = 0;
//        var iterator = property.Copy();
//        iterator.NextVisible(true); // Move to the first child property
//        do
//        {
//            totalHeight += EditorGUI.GetPropertyHeight(iterator, true) + EditorGUIUtility.standardVerticalSpacing;
//        } while (iterator.NextVisible(false));
//        return totalHeight;
//    }

//    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
//    {
//        // Draw each field without the foldout
//        var iterator = property.Copy();
//        iterator.NextVisible(true); // Move to the first child property
//        position.height = EditorGUIUtility.singleLineHeight;
//        do
//        {
//            EditorGUI.PropertyField(position, iterator, true);
//            position.y += EditorGUI.GetPropertyHeight(iterator, true) + EditorGUIUtility.standardVerticalSpacing;
//        } while (iterator.NextVisible(false));
//    }
//}
