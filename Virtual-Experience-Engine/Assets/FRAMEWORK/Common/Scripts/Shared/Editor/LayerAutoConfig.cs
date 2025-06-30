#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;

namespace VE2.Common.Shared
{
    public static class RequiredProjectSettings
    {
        public static readonly (string name, int index)[] RequiredLayers = new[]
        {
            ("Ground", 3),
            ("V_Player", 6),
            ("V_Layer0", 7),
            ("V_Layer1", 8),
            ("V_Layer2", 9),
            ("V_Layer3", 10),
            ("V_Layer4", 11),
            ("V_Layer5", 12),
            ("V_Layer6", 13),
            ("V_Layer7", 14),
            ("V_Layer8", 15),
            ("V_Layer9", 16),
        };

        public const int BuiltInLayerLimit = 6;

        public static readonly string[] RequiredTags = new[]
        {
            "V_Tag0", "V_Tag1", "V_Tag2", "V_Tag3", "V_Tag4",
            "V_Tag5", "V_Tag6", "V_Tag7", "V_Tag8", "V_Tag9"
        };

        public static readonly string[] BuiltInUnityTags = new[]
        {
            "Untagged", "Respawn", "Finish", "EditorOnly",
            "MainCamera", "Player", "GameController"
        };

        public static readonly HashSet<string> RequiredLayerNames = new HashSet<string>(
            RequiredLayers.Select(l => l.name));
    }

    internal class LayerAutoConfig
    {
        [MenuItem("VE2/Configure Layers and Tags", priority = 0)]
        internal static void ShowWindow()
        {
            var window = ScriptableObject.CreateInstance<VE2LayerAutoConfig>();
            window.position = new Rect(Screen.width / 2, Screen.height / 2, 300, 120);
            window.titleContent = new GUIContent("Configure Layers & Tags");
            window.Show();
        }
    }

    internal class VE2LayerAutoConfig : EditorWindow
    {
        private void OnGUI()
        {
            EditorGUILayout.HelpBox(
                "This will assign required VE2 layers to specific indices, and add VE2 tags, " +
                "All other layers and tags (aside from those built-in to Unity will be removed",
                MessageType.Warning);

            if (GUILayout.Button("Configure VE2 Layers & Tags"))
            {
                SetupScene();
            }
        }

        private void SetupScene()
        {
            var tagManagerAsset = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0];
            var tagManager = new SerializedObject(tagManagerAsset);
            var layers = tagManager.FindProperty("layers");
            var tagsProp = tagManager.FindProperty("tags");

            Undo.RegisterCompleteObjectUndo(tagManagerAsset, "Configure VE2 Layers & Tags");

            bool modified = false;

            // === Configure Required Layers ===
            foreach (var (name, index) in RequiredProjectSettings.RequiredLayers)
            {
                if (index < 0 || index > 31)
                {
                    Debug.LogError($"Invalid layer index: {index} for '{name}'. Must be between 0 and 31.");
                    continue;
                }

                var layerProp = layers.GetArrayElementAtIndex(index);
                if (layerProp == null)
                {
                    Debug.LogError($"Layer index {index} could not be accessed.");
                    continue;
                }

                string currentValue = layerProp.stringValue;

                bool isUnityReserved = index == 0 || index == 1 || index == 2 || index == 4 || index == 5;

                if (isUnityReserved)
                {
                    Debug.LogWarning($"Cannot set layer '{name}' at index {index}: reserved by Unity as '{currentValue}'");
                    continue;
                }

                if (currentValue == name)
                {
                    Debug.Log($"Layer '{name}' already set at index {index}.");
                    continue;
                }

                if (!string.IsNullOrEmpty(currentValue))
                {
                    Debug.LogWarning($"Layer index {index} is occupied by '{currentValue}', skipping assignment of '{name}'.");
                    continue;
                }

                layerProp.stringValue = name;
                Debug.Log($"Assigned layer '{name}' to index {index}.");
                modified = true;
            }

            // === Add Required Tags ===
            foreach (var tag in RequiredProjectSettings.RequiredTags)
            {
                bool tagExists = false;
                for (int i = 0; i < tagsProp.arraySize; i++)
                {
                    if (tagsProp.GetArrayElementAtIndex(i).stringValue == tag)
                    {
                        tagExists = true;
                        break;
                    }
                }

                if (!tagExists)
                {
                    tagsProp.InsertArrayElementAtIndex(tagsProp.arraySize);
                    tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1).stringValue = tag;
                    Debug.Log($"Added tag '{tag}'.");
                    modified = true;
                }
            }

            // === Remove Unused Tags ===
            for (int i = tagsProp.arraySize - 1; i >= 0; i--)
            {
                string tag = tagsProp.GetArrayElementAtIndex(i).stringValue;

                if (!RequiredProjectSettings.RequiredTags.Contains(tag) &&
                    !RequiredProjectSettings.BuiltInUnityTags.Contains(tag))
                {
                    Debug.Log($"Removing unused tag '{tag}'.");
                    tagsProp.DeleteArrayElementAtIndex(i);
                    modified = true;
                }
            }

            // === Remove Unused Custom Layers ===
            for (int i = 0; i < layers.arraySize; i++)
            {
                bool isUnityReserved = i == 0 || i == 1 || i == 2 || i == 4 || i == 5;
                var layerProp = layers.GetArrayElementAtIndex(i);

                if (layerProp == null || string.IsNullOrEmpty(layerProp.stringValue))
                    continue;

                string current = layerProp.stringValue;

                if (RequiredProjectSettings.RequiredLayers.Any(x => x.index == i && x.name == current))
                    continue;

                if (isUnityReserved)
                    continue;

                Debug.Log($"Clearing unused custom layer '{current}' at index {i}.");
                layerProp.stringValue = string.Empty;
                modified = true;
            }

            // === Apply and Final Log ===
            if (modified)
            {
                tagManager.ApplyModifiedProperties();
                Debug.Log("VE2 layer/tag configuration complete. Changes can be undone via Ctrl+Z / Cmd+Z.");
            }
            else
            {
                Debug.Log("No changes made. All required layers and tags already set.");
            }
        }
    }
}
#endif
