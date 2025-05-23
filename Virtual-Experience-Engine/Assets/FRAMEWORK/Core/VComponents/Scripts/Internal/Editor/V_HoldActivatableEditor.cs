#if UNITY_EDITOR
using Toolbox.Editor;
using UnityEditor;
using UnityEngine;
using VE2.Core.VComponents.Internal;

namespace VE2.Core.VComponents.Internal
{
    [CustomEditor(typeof(V_HoldActivatable))]
    internal class V_HoldActivatableEditor : ToolboxEditor
    {
        public override void DrawCustomInspector()
        {
            V_HoldActivatable holdActivatable = (V_HoldActivatable)target;
            Collider collider = holdActivatable.Collider;

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