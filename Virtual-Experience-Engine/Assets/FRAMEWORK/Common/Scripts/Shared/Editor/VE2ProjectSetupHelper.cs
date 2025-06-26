#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;
using DarkRift.Server;
using System.Linq;

namespace VE2.Core.Common
{
    internal class VE2ProjectSetupHelper
    {
        [MenuItem("VE2/Create Plugin Assembly Definition", priority = -1)]
        public static void CreateAsmdef()
        {
            string asmdefName = "PluginAssemblyYOUR_PLUGIN_NAME";
            string folderPath = "Assets/Scripts";

            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);

            string asmdefPath = Path.Combine(folderPath, asmdefName + ".asmdef");

            if (File.Exists(asmdefPath))
            {
                Debug.LogWarning("ASMDEF already exists at: " + asmdefPath);
                return;
            }

            string[] references = new[] {
    "Unity.TextMeshPro",
    "Unity.InputSystem",
    "VE2.Common.API",
    "VE2.Common.Shared",
    "VE2.Core.VComponents.API",
    "VE2.Core.Player.API",
    "VE2.Core.UI.API",
    "VE2.NonCore.Instancing.API",
    "VE2.NonCore.Platform.API",
};

            string referencesJson = string.Join(",\n        ", references.Select(r => $"\"{r}\""));

            string json = $@"{{
    ""name"": ""{asmdefName}"",
    ""references"": [{referencesJson}],
    ""includePlatforms"": [],
    ""excludePlatforms"": [],
    ""allowUnsafeCode"": false,
    ""overrideReferences"": false,
    ""precompiledReferences"": [],
    ""autoReferenced"": true,
    ""defineConstraints"": [],
    ""versionDefines"": [],
    ""noEngineReferences"": false
}}";

            File.WriteAllText(asmdefPath, json);
            AssetDatabase.Refresh();
            Debug.Log("Created .asmdef at: " + asmdefPath);
        }

    }
}
#endif