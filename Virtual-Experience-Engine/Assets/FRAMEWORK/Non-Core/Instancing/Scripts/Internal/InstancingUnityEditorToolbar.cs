#if UNITY_EDITOR

using Toolbox.Editor;
using UnityEngine;
using System;
using VE2.Core.Player;

namespace VE2.NonCore.Instancing.Internal
{
    [UnityEditor.InitializeOnLoad]
    internal static class VE2UnityEditorToolbar
    {
        public static event Action OnPreferVRClicked;
        public static event Action OnPrefer2DClicked;

        public static bool PreferVRMode {get; private set;}


        private static V_InstanceIntegration _instancingProvider = null;

        static VE2UnityEditorToolbar()
        {
            ToolboxEditorToolbar.OnToolbarGuiRight  += OnToolbarGui;
        }

        private static void OnToolbarGui()
        {
            GUILayout.FlexibleSpace();

            // Show button, and trigger method on click
            if (GUILayout.Button("VE2: Start Local Server"))
                InstancingUtils.BootLocalServerIfNotAlreadyRunning();

            // Make sure the player spawner is found
            if (_instancingProvider == null)
                _instancingProvider = GameObject.FindFirstObjectByType<V_InstanceIntegration>();

            if (_instancingProvider == null)
                return;

            //Show button, if clicked, call BootLocalServerIfNeeded
        }

        // private static void KillLocalServerIfNeeded()
        // {
        //     string processName = "DarkRift.Server.Console";
        //     foreach (Process process in Process.GetProcessesByName(processName))
        //     {
        //         process.Kill();
        //     }
        // }
    }
}
#endif  
