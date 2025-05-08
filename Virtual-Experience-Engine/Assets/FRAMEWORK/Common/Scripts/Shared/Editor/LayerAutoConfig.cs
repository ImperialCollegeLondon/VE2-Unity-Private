#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace VE2.Common.Shared
{
    public static class RequiredProjectSettings
{
    public static readonly string[] RequiredLayers = new[]
    {
        "Default",
        "TransparentFX",
        "Ignore Raycast",
        "Ground",
        "Water",
        "UI",
        "V_Player",
        "V_Layer0",
        "V_Layer1",
        "V_Layer2",
        "V_Layer3",
        "V_Layer4",
        "V_Layer5",
        "V_Layer6",
        "V_Layer7",
        "V_Layer8",
        "V_Layer9",
    };
}

    internal class LayerAutoConfig
    {
        [MenuItem("VE2/Configure Layers", priority = 0)]
        internal static void ShowWindow()
        {
            // Create a new window instance
            var window = ScriptableObject.CreateInstance<VE2LayerAutoConfig>();
            window.position = new Rect(Screen.width / 2, Screen.height / 2, 200, 100);
            window.titleContent = new GUIContent("Configure Layers");
            
            // Show the window
            window.Show();
        }
    }

    internal class VE2LayerAutoConfig : EditorWindow
    {
        private void OnGUI()
        {
            EditorGUILayout.HelpBox("This will remove ALL GameObjects in the scene and instantiate the basic VE2 utilities", (UnityEditor.MessageType)MessageType.Info);

            if (GUILayout.Button("Setup Scene"))
            {
                SetupScene();
            }
        }

        private void SetupScene()
        {
            var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var layers = tagManager.FindProperty("layers");

            foreach (string requiredLayer in RequiredProjectSettings.RequiredLayers)
            {
                if (!LayerExists(requiredLayer, layers))
                {
                    for (int i = 8; i < layers.arraySize; i++)
                    {
                        var prop = layers.GetArrayElementAtIndex(i);
                        if (string.IsNullOrEmpty(prop.stringValue))
                        {
                            prop.stringValue = requiredLayer;
                            Debug.Log($"Added layer: {requiredLayer}");
                            break;
                        }
                    }
                }
            }

            tagManager.ApplyModifiedProperties();
        }

        // private static void CheckLayers()
        // {
        //     var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        //     var layersProp = tagManager.FindProperty("layers");

        //     var missing = RequiredProjectSettings.RequiredLayers
        //         .Where(layerName => !LayerExists(layerName, layersProp))
        //         .ToList();

        //     if (missing.Count > 0)
        //     {
        //         Debug.LogWarning($"Missing required layers: {string.Join(", ", missing)}\n" +
        //                         "Use Tools > VE2 > Fix Project Layers to add them.");
        //     }
        // }

        private static bool LayerExists(string name, SerializedProperty layers)
        {
            for (int i = 8; i < layers.arraySize; i++)
            {
                var prop = layers.GetArrayElementAtIndex(i);
                if (prop != null && prop.stringValue == name)
                    return true;
            }
            return false;
        }
    }
}
#endif
