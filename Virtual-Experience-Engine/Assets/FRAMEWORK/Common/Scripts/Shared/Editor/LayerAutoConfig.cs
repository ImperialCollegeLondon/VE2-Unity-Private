#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System;

namespace VE2.Common.Shared
{
    public static class RequiredProjectSettings
    {
        public static readonly (string name, int index)[] RequiredLayers = new[]
        {
            ("Ground", 3),         // Unity oddly leaves 3 free, so we’ll use it
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
    }

    internal class LayerAutoConfig
    {
        [MenuItem("VE2/Configure Layers", priority = 0)]
        internal static void ShowWindow()
        {
            var window = ScriptableObject.CreateInstance<VE2LayerAutoConfig>();
            window.position = new Rect(Screen.width / 2, Screen.height / 2, 300, 120);
            window.titleContent = new GUIContent("Configure Layers");
            window.Show();
        }
    }

    internal class VE2LayerAutoConfig : EditorWindow
    {
        private void OnGUI()
        {
            EditorGUILayout.HelpBox(
                "This will assign required layers to specific indices.\n\n" +
                "Existing non-empty layers at required indices will NOT be overwritten.",
                MessageType.Warning);

            if (GUILayout.Button("Configure VE2 Layers"))
            {
                SetupScene();
            }
        }

        private void SetupScene()
        {
            var tagManagerAsset = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0];
            var tagManager = new SerializedObject(tagManagerAsset);
            var layers = tagManager.FindProperty("layers");

            // Register undo for tag manager
            Undo.RegisterCompleteObjectUndo(tagManagerAsset, "Configure VE2 Layers");

            var modified = false;

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
                    Debug.LogWarning(
                        $"Layer index {index} is occupied by '{currentValue}', skipping assignment of '{name}'.");
                    continue;
                }

                // Assign and mark modified
                layerProp.stringValue = name;
                Debug.Log($"Assigned layer '{name}' to index {index}.");
                modified = true;
            }

            if (modified)
            {
                tagManager.ApplyModifiedProperties();
                Debug.Log("Layer configuration complete. Changes can be undone via Ctrl+Z / Cmd+Z.");
            }
            else
            {
                Debug.Log("No changes made to layer configuration.");
            }
        }
    }
}
#endif
