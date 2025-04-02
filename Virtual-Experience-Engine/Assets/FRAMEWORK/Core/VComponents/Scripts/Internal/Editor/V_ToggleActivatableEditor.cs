#if UNITY_EDITOR
using Toolbox.Editor;
using UnityEditor;
using UnityEngine;
using VE2.Core.VComponents.Internal;

namespace VE2.Core.VComponents.Internal
{
    [CustomEditor(typeof(V_ToggleActivatable))]
    public class V_ToggleActivatableEditor : ToolboxEditor
    {
        public override void DrawCustomInspector()
        {
            V_ToggleActivatable toggleActivatable = (V_ToggleActivatable)target;
            Collider collider = toggleActivatable.Collider;

            if (collider == null || collider.isTrigger)
            {
                string error = "";

                    if (collider == null)
                        error += "This GameObject requires a Collider\n";
                    else if (collider.isTrigger)
                        error += "This GameObject's Collider cannot be a trigger\n";

                if (error.EndsWith("\n"))
                    error = error.Remove(error.Length - 1);

                EditorGUILayout.HelpBox(error, MessageType.Error);
            }

            base.DrawCustomInspector();
        }
    }
}
#endif
