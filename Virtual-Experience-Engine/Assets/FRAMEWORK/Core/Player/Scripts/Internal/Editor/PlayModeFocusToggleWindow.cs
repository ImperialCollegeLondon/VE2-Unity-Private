#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

namespace VE2.Core.Player.Internal
{
    public class PlayModeFocusToggleWindow : EditorWindow
    {
        private static bool isEnabled = true;

        [MenuItem("Window/Play Mode Focus Toggle")]
        public static void ShowWindow()
        {
            GetWindow<PlayModeFocusToggleWindow>("Play mode entry override");
        }

        private void OnGUI()
        {
            GUILayout.Label("Enable this if, on play, you want to show the game view, but leave it unfocused - Unity doesn't seem to support this out of the box \n " +
                "When enabled, the view will return to the scene view when exiting play mode", EditorStyles.helpBox);

            isEnabled = EditorGUILayout.Toggle("Enabled", isEnabled);

            if (GUI.changed)
            {
                PlayModeFocusHandler.SetEnabled(isEnabled);
            }
        }
    }

    [InitializeOnLoad]
    public class PlayModeFocusHandler
    {
        private static bool isEnabled = true;

        static PlayModeFocusHandler()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        public static void SetEnabled(bool enabled)
        {
            isEnabled = enabled;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (!isEnabled)
                return;

            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                EditorApplication.ExecuteMenuItem("Window/General/Game");
                FocusInspector(); //So we unfocus the game view
            }
            else if (state == PlayModeStateChange.ExitingPlayMode)
            {
                EditorApplication.ExecuteMenuItem("Window/General/Scene"); //Back to the scene view 
            }
        }

        private static void FocusInspector()
        {
            var inspectorWindow = EditorWindow.GetWindow(typeof(Editor).Assembly.GetType("UnityEditor.InspectorWindow"));
            if (inspectorWindow != null)
            {
                inspectorWindow.Focus();
            }
            else
            {
                Debug.LogWarning("No active Inspector window found.");
            }
        }
    }
}
# endif