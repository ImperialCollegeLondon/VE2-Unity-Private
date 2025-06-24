#if UNITY_EDITOR
using UnityEditor;
using VE2.Core.UI.Internal;
using UnityEditor.UI;

[CustomEditor(typeof(V_ScrollRect))]
public class V_ScrollRectEditor : ScrollRectEditor
{
    SerializedProperty _dragSensitivity;

    protected override void OnEnable()
    {
        base.OnEnable();
        _dragSensitivity = serializedObject.FindProperty("_dragSensitivity");
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        serializedObject.Update();
        EditorGUILayout.HelpBox("Sensitivity when dragging the scrollbar, not to be confused with scroll sensitivity.", MessageType.Info);
        EditorGUILayout.PropertyField(_dragSensitivity);
        serializedObject.ApplyModifiedProperties();
    }
}
#endif