#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace VE2.Common.Shared
{
    internal class VE2SetupAndroidManifest
    {
        public static void CopyManifestFromResources()
        {
            // Load the TextAsset from Resources
            TextAsset manifestAsset = Resources.Load<TextAsset>("AndroidManifestPlugin");
            if (manifestAsset == null)
            {
                Debug.LogError("Could not find AndroidManifestPlugin.xml in Resources.");
                return;
            }

            string manifestContent = manifestAsset.text;

            string pluginsAndroidPath = "Assets/Plugins/Android";
            string targetPath = Path.Combine(pluginsAndroidPath, "AndroidManifest.xml");

            // Ensure destination directory exists
            if (!Directory.Exists(pluginsAndroidPath))
                Directory.CreateDirectory(pluginsAndroidPath);

            // Backup existing manifest if needed
            if (File.Exists(targetPath))
            {
                string backupPath = Path.Combine(pluginsAndroidPath, "AndroidManifestOld.xml");
                int counter = 1;

                // Keep trying until we find a free backup file name
                while (File.Exists(backupPath))
                {
                    counter++;
                    backupPath = Path.Combine(pluginsAndroidPath, $"AndroidManifestOld{counter}.xml");
                }

                File.Move(targetPath, backupPath);
                Debug.Log($"Existing AndroidManifest.xml backed up to {backupPath}");
            }

            // Write the new manifest
            File.WriteAllText(targetPath, manifestContent);
            Debug.Log($"New AndroidManifest.xml copied to {targetPath}");

            // Refresh the AssetDatabase so Unity sees the new file
            AssetDatabase.Refresh();
        }
    }
}
#endif
