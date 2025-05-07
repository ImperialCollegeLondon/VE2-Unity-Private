#if UNITY_EDITOR

using Toolbox.Editor;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using System;
using System.Reflection;
using VE2.Core.Player.Internal;
using VE2.Core.Player.API;
using VE2.Common.API;

namespace VE2.Core.Player
{
    [UnityEditor.InitializeOnLoad]
    internal static class VE2UnityEditorToolbar
    {
        private static V_PlayerSpawner _playerSpawner = null;

        static VE2UnityEditorToolbar()
        {
            ToolboxEditorToolbar.OnToolbarGuiLeft  += OnToolbarGui;
        }

        private static void OnToolbarGui()
        {
            GUILayout.FlexibleSpace();

            // Make sure the player spawner is found
            if (_playerSpawner == null)
                _playerSpawner = GameObject.FindFirstObjectByType<V_PlayerSpawner>();

            if (_playerSpawner == null)
                return;

            // Determine if the button should be interactable
            bool interactable = _playerSpawner._playerConfig.PlayerModeConfig.Enable2D && _playerSpawner._playerConfig.PlayerModeConfig.EnableVR;

            // Save the current GUI enabled state, and set it to false if the button is not interactable
            bool originalGUIState = GUI.enabled;
            GUI.enabled = interactable;

            string buttonText = "VE2: ";
            if (interactable)
            {
                buttonText += VE2API.PreferVRMode ? "Preferring VR" : "Preferring 2D";
            }
            else
            {
                VE2API.PreferVRMode = _playerSpawner._playerConfig.PlayerModeConfig.EnableVR;
                buttonText += _playerSpawner._playerConfig.PlayerModeConfig.EnableVR ? "VR Only" : "2D Only";
            }

            // Create the toggle button
            if (GUILayout.Toggle(VE2API.PreferVRMode, buttonText, "Button"))
            {
                if (!VE2API.PreferVRMode)
                    VE2API.PreferVRMode = true;
            }
            else
            {
                if (VE2API.PreferVRMode)
                    VE2API.PreferVRMode = false;
            }

            // Restore the original GUI enabled state
            GUI.enabled = originalGUIState;
        }
    }
}
#endif  
