#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace VE2.Common.Shared
{
    internal class VE2AutoEditorToolboxSetup
    {
        const string settingsAssetPath = "Packages/com.browar.editor-toolbox/EditorSettings.asset";

        //[MenuItem("VE2/Toolbox setup", priority = -998)]
        internal static void CreateToolboxEditorSettingsAsset()
        {
            const string folderPath = "Assets/Settings";
            const string assetPath = folderPath + "/ToolboxEditorSettings.asset";

            // Ensure the Settings folder exists
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                AssetDatabase.CreateFolder("Assets", "Settings");
            }

            // If it already exists, skip
            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath) != null)
            {
                Debug.Log("ToolboxEditorSettings already exists at " + assetPath);
                return;
            }

            // Find the internal ToolboxEditorSettings type
            var toolboxSettingsType = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a =>
                {
                    // Avoid reflection exceptions in dynamic assemblies
                    try { return a.GetTypes(); }
                    catch { return Type.EmptyTypes; }
                })
                .FirstOrDefault(t => t.Name == "ToolboxEditorSettings" && typeof(ScriptableObject).IsAssignableFrom(t));

            if (toolboxSettingsType == null)
            {
                Debug.LogError("Could not find ToolboxEditorSettings type via reflection.");
                return;
            }

            // Create instance
            var instance = ScriptableObject.CreateInstance(toolboxSettingsType);
            if (instance == null)
            {
                Debug.LogError("Failed to create instance of ToolboxEditorSettings.");
                return;
            }

            // Save the asset
            AssetDatabase.CreateAsset(instance, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("✅ ToolboxEditorSettings created at " + assetPath);
        }


        // internal static void ConfigureEditorToolbox()
        // {
        //     Debug.Log("Configuring Editor Toolbox...");

        //     // Load the settings asset
        //     var settingsAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(settingsAssetPath);
        //     if (settingsAsset == null)
        //     {
        //         Debug.LogWarning("Editor Toolbox settings not found. Is the EditorToolbox package installed?");
        //         return;
        //     }

        //     // Use reflection to find the correct type of the settings asset
        //     Type targetSettingsType = settingsAsset.GetType();

        //     // Confirm it is the expected one by name (in case it's a proxy type)
        //     if (!targetSettingsType.FullName.Contains("TargetTypeDrawerSettings"))
        //     {
        //         // Attempt to find it manually if necessary
        //         targetSettingsType = AppDomain.CurrentDomain.GetAssemblies()
        //             .SelectMany(a => a.GetTypes())
        //             .FirstOrDefault(t => t.FullName == "EditorToolbox.Settings.TargetTypeDrawerSettings");

        //         if (targetSettingsType == null)
        //         {
        //             Debug.LogWarning("Could not find the 'EditorToolbox.Settings.TargetTypeDrawerSettings' type."); 
        //             return;
        //         }
        //     }

        //     // Get the Targets property
        //     var targetsProperty = targetSettingsType.GetProperty("Targets", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        //     if (targetsProperty == null)
        //     {
        //         Debug.LogWarning("Could not reflect Targets property on TargetTypeDrawerSettings.");
        //         return;
        //     }

        //     // Get the current list
        //     var targetsList = targetsProperty.GetValue(settingsAsset) as System.Collections.IList;
        //     if (targetsList == null)
        //     {
        //         Debug.LogWarning("Targets property is not a list.");
        //         return;
        //     }

        //     // Clear and repopulate
        //     targetsList.Clear();

        //     var elementType = targetsList.GetType().GetGenericArguments().FirstOrDefault();
        //     if (elementType == null)
        //     {
        //         Debug.LogWarning("Could not determine element type of Targets list.");
        //         return;
        //     }

        //     var allTypes = AppDomain.CurrentDomain.GetAssemblies()
        //         .SelectMany(a => a.GetTypes())
        //         .Where(t =>
        //             typeof(UnityEngine.Object).IsAssignableFrom(t) &&
        //             !t.IsAbstract &&
        //             !t.IsGenericType &&
        //             !typeof(UnityEditor.EditorWindow).IsAssignableFrom(t))
        //         .ToArray();

        //     foreach (var type in allTypes)
        //     {
        //         object instance = Activator.CreateInstance(elementType);
        //         var typeField = elementType.GetField("Type", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        //         var enabledField = elementType.GetField("Enabled", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        //         if (typeField != null) typeField.SetValue(instance, type);
        //         if (enabledField != null) enabledField.SetValue(instance, true);

        //         targetsList.Add(instance);
        //     }

        //     targetsProperty.SetValue(settingsAsset, targetsList);
        //     EditorUtility.SetDirty(settingsAsset);
        //     AssetDatabase.SaveAssets();

        //     Debug.Log($"✅ Editor Toolbox configured with {allTypes.Length} target types.");
        // }


    }
}

#endif
