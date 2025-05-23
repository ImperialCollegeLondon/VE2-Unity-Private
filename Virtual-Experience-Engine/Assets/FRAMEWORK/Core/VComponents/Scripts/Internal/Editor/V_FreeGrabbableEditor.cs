#if UNITY_EDITOR
using Toolbox.Editor;
using UnityEditor;
using UnityEngine;

namespace VE2.Core.VComponents.Internal
{
    [CustomEditor(typeof(V_FreeGrabbable))]
    internal class V_FreeGrabbableEditor : ToolboxEditor
    {
        public override void DrawCustomInspector()
        {
            V_FreeGrabbable freeGrababble = (V_FreeGrabbable)target;

            Collider collider = freeGrababble.Collider;
            Rigidbody rigidbody = freeGrababble.Rigidbody;

            if (collider == null || collider.isTrigger || rigidbody == null)
            {
                string error = "";

                    if (collider == null)
                        error += "This GameObject requires a Collider\n";
                    else if (collider.isTrigger)
                        error += "This GameObject's Collider cannot be a trigger\n";
                    if (rigidbody == null)
                        error += "This GameObject requires a Rigidbody\n";

                if (error.EndsWith("\n"))
                    error = error.Remove(error.Length - 1);

                EditorGUILayout.HelpBox(error, MessageType.Error);
            }

            base.DrawCustomInspector();
        }
    }
}
#endif