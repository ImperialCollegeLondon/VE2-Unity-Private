#if UNITY_EDITOR
using Toolbox.Editor;
using UnityEditor;
using UnityEngine;
using VE2.Core.VComponents.Internal;

namespace VE2.Core.VComponents.Internal
{
    //TODO: Implement! Needs to be smarter than just "does the attach point have a collider"
    // [CustomEditor(typeof(V_LinearAdjustable))]
    // internal class V_LinearAdjustableEditor : ToolboxEditor
    // {
    //     public override void DrawCustomInspector()
    //     {
    //         V_LinearAdjustable linearAdjustable = (V_LinearAdjustable)target;
    //         Collider collider = linearAdjustable.Collider;

    //         if (collider == null || collider.isTrigger)
    //         {
    //             string error = "";

    //                 if (collider == null)
    //                     error += $"The attach point ({linearAdjustable.AttachPointGOName}) requires a Collider\n";
    //                 else if (collider.isTrigger)
    //                     error += $"The attach point's ({linearAdjustable.AttachPointGOName}) Collider cannot be a trigger\n";

    //             if (error.EndsWith("\n"))
    //                 error = error.Remove(error.Length - 1);

    //             EditorGUILayout.HelpBox(error, MessageType.Error);
    //         }

    //         base.DrawCustomInspector();
    //     }
    // }
}
#endif