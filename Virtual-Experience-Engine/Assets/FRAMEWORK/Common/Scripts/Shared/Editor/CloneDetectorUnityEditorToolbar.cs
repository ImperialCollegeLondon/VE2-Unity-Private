#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using Toolbox.Editor;

namespace VE2.Common.Shared.Editor
{   
    [InitializeOnLoad]
    public static class CloneDetectorUnityEditorToolbar
    {
        static CloneDetectorUnityEditorToolbar()
        {
            ToolboxEditorToolbar.OnToolbarGuiRight += OnToolbarGui;
        }

        private static void OnToolbarGui()
        {
            GUILayout.FlexibleSpace();

            string projectName = Application.productName;
            string projectRoot = System.IO.Directory.GetParent(Application.dataPath).FullName;
            string folderName = System.IO.Path.GetFileName(projectRoot);

            if (!string.IsNullOrEmpty(folderName) && System.Text.RegularExpressions.Regex.IsMatch(folderName, @"_clone_\d+$"))
            {
                GUIStyle warningLabel = new GUIStyle(EditorStyles.boldLabel);
                warningLabel.normal.textColor = Color.yellow;
                GUILayout.Label($"This is a clone instance, Don't save changes here!", warningLabel);
            }
        }
    }
}
#endif