using System.Diagnostics;
using System.IO;
using UnityEngine;

namespace VE2.NonCore.Instancing.Internal
{
    internal class InstancingUtils
    {
        internal static void BootLocalServerIfNotAlreadyRunning()
        {
            #if UNITY_EDITOR
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

                string assetPath = UnityEditor.AssetDatabase.GetAssetPath(instancingProjectReferences.LocalServerExecutable);
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
                #endif
            }
        }
    }
}
