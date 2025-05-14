#if UNITY_EDITOR
using Toolbox.Editor;
using UnityEditor;
using UnityEngine;

namespace VE2.Core.VComponents.Internal
{
    //TODO: Implement! Needs to be smarter than just "does the attach point have a collider"
    // [CustomEditor(typeof(V_RotationalAdjustable))]
    // internal class V_RotationalAdjustableEditor : ToolboxEditor
    // {
    //     public override void DrawCustomInspector()
    //     {
    //         V_RotationalAdjustable rotationalActivatable = (V_RotationalAdjustable)target;
    //         Collider collider = rotationalActivatable.Collider;

    //         if (collider == null || collider.isTrigger)
    //         {
    //             string error = "";

    //                 if (collider == null)
    //                     error += $"The attach point ({rotationalActivatable.AttachPointGOName}) requires a Collider\n";
    //                 else if (collider.isTrigger)
    //                     error += $"The attach point's ({rotationalActivatable.AttachPointGOName}) Collider cannot be a trigger\n";

    //             if (error.EndsWith("\n"))
    //                 error = error.Remove(error.Length - 1);

    //             EditorGUILayout.HelpBox(error, MessageType.Error);
    //         }

    //         base.DrawCustomInspector();
    //     }
    // }
}
#endif