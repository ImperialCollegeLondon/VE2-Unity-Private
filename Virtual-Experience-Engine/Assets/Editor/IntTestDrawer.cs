using Toolbox.Editor.Drawers;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(int))]
public class IntTestDrawerUnity : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        Debug.Log("IntTestDrawer called");

        // Draw the default int field at the given position
        EditorGUI.PropertyField(position, property, label);
    }
}


public class IntTestDrawer : ToolboxTargetTypeDrawer
{
    public override void OnGui(SerializedProperty property, GUIContent label)
    {
        Debug.Log("IntTestDrawer called");
        EditorGUILayout.PropertyField(property, label);
    }

    public override System.Type GetTargetType() => typeof(int);
    public override bool UseForChildren() => false;
}

public class FloatTestDrawer : ToolboxTargetTypeDrawer
{
    public override void OnGui(SerializedProperty property, GUIContent label)
    {
        Debug.Log("FloatTestDrawer called");
        EditorGUILayout.PropertyField(property, label);
    }

    public override System.Type GetTargetType() => typeof(float);
    public override bool UseForChildren() => false;
}
