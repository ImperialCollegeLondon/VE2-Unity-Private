#if UNITY_EDITOR

using Toolbox.Editor;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using System;
using System.Reflection;
using VE2.Core.Player.Internal;
using System.Diagnostics;
using System.IO;
using VE2.NonCore.Instancing.Internal;

namespace VE2.Core.Player
{
    [UnityEditor.InitializeOnLoad]
    public static class VE2UnityEditorToolbar
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
                BootLocalServerIfNeeded();

            // Make sure the player spawner is found
            if (_instancingProvider == null)
                _instancingProvider = GameObject.FindFirstObjectByType<V_InstanceIntegration>();

            if (_instancingProvider == null)
                return;

            //Show button, if clicked, call BootLocalServerIfNeeded
        }

        private static void BootLocalServerIfNeeded()
        {
            Process[] pname = Process.GetProcessesByName("DarkRift.Server.Console");

            bool serverAlreadyRunning = pname.Length > 0;

            if (!serverAlreadyRunning)
            {
                InstancingProjectReferences instancingProjectReferences = Resources.Load<InstancingProjectReferences>("InstancingProjectReferences");
                if (instancingProjectReferences == null)
                {
                    UnityEngine.Debug.LogError("Couldn't get path to exe");
                    return;
                }

                string assetPath = AssetDatabase.GetAssetPath(instancingProjectReferences.LocalServerExecutable);
                string fullPath = Path.GetFullPath(assetPath);

                if (File.Exists(fullPath) == false)
                {
                    UnityEngine.Debug.LogError($"Couldn't find local server executable");
                    return;
                }

                UnityEngine.Debug.Log($"Launching local server...");

                ProcessStartInfo startInfo = new();
                startInfo.WorkingDirectory = Path.GetDirectoryName(fullPath);
                startInfo.FileName = fullPath;
                Process.Start(startInfo);
            }
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
