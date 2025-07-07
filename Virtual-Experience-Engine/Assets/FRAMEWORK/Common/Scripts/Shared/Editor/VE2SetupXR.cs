#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.XR.Management;
using UnityEngine;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features;
using UnityEngine.XR.OpenXR.Features.Interactions;

namespace VE2.Common.Shared
{
    internal static class VE2SetupXR
    {
        private const string GeneralSettingsPath = "Assets/XR/XRGeneralSettingsPerBuildTarget.asset";
        private const string LoaderAssetPath = "Assets/XR/Loaders/OpenXRLoader.asset";

        public static void EnableXRPlugInManagement()
        {
            // Enable OpenXR loader in XR Plugin Management
            EnableOpenXRLoader(BuildTargetGroup.Standalone);
            EnableOpenXRLoader(BuildTargetGroup.Android);
        }

        public static void EnableOpenXRFeatures()
        {
            EnableOpenXRTouchSupport(BuildTargetGroup.Standalone);
            EnableOpenXRTouchSupport(BuildTargetGroup.Android);
        }

        private static void EnableOpenXRLoader(BuildTargetGroup buildTargetGroup)
        {
            // Ensure directory exists
            var loaderDirectory = Path.GetDirectoryName(LoaderAssetPath);
            if (!Directory.Exists(loaderDirectory))
            {
                Directory.CreateDirectory(loaderDirectory);
                AssetDatabase.Refresh();
            }

            // Load or create persistent loader asset
            var loader = AssetDatabase.LoadAssetAtPath<XRLoader>(LoaderAssetPath);
            if (loader == null)
            {
                var loaderType = typeof(UnityEngine.XR.OpenXR.OpenXRLoader);
                loader = ScriptableObject.CreateInstance(loaderType) as XRLoader;
                AssetDatabase.CreateAsset(loader, LoaderAssetPath);
                AssetDatabase.SaveAssets();
                Debug.Log("Created persistent OpenXRLoader asset.");
            }

            // Load XRGeneralSettings
            var generalSettingsAsset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(GeneralSettingsPath);
            var generalSettingsInstance = generalSettingsAsset as XRGeneralSettingsPerBuildTarget;
            var xrSettings = generalSettingsInstance?.ManagerSettingsForBuildTarget(buildTargetGroup);

            if (xrSettings == null)
            {
                Debug.LogError($"XRManagerSettings not found for {buildTargetGroup}");
                return;
            }

            // Add loader if not already present
            var openXRLoaderType = typeof(UnityEngine.XR.OpenXR.OpenXRLoader);
            if (!xrSettings.activeLoaders.Any(l => l != null && l.GetType() == openXRLoaderType))
            {
                xrSettings.TryAddLoader(loader, (int)buildTargetGroup);
                EditorUtility.SetDirty(xrSettings);
                AssetDatabase.SaveAssets();
                Debug.Log("OpenXR Loader added and saved.");
            }
            else
            {
                Debug.Log("OpenXR Loader already present.");
            }

            EditorUtility.RequestScriptReload();
        }

        // public static void DebugXRSettings()
        // {
        //     var generalSettings = XRGeneralSettings.Instance;
        //     if (generalSettings == null)
        //     {
        //         Debug.LogError("❌ XRGeneralSettings.Instance is null");
        //         return;
        //     }

        //     var manager = generalSettings.Manager;
        //     if (manager == null)
        //     {
        //         Debug.LogError("❌ XRManagerSettings is null");
        //         return;
        //     }

        //     Debug.Log("✅ XR General Settings:");
        //     Debug.Log($"- Init Manager On Start: {generalSettings.InitManagerOnStart}");
        //     Debug.Log($"- Loaders Count: {manager.loaders.Count}");
        //     Debug.Log($"- Active Loader: {manager.activeLoader?.name ?? "None"}");

        //     foreach (var loader in manager.loaders)
        //     {
        //         Debug.Log($"   - Loader: {loader.name} ({loader.GetType().FullName})");
        //     }

        //     Debug.Log($"Build target group: {EditorUserBuildSettings.selectedBuildTargetGroup}");
        // }

        // public class FindXRGeneralSettingsAsset
        // {
        //     [MenuItem("VE2/Find XRGeneralSettings Asset")]
        //     private static void FindSettingsAsset()
        //     {
        //         string[] guids = AssetDatabase.FindAssets("t:XRGeneralSettings");
        //         if (guids.Length == 0)
        //         {
        //             Debug.LogWarning("No XRGeneralSettings asset found in project.");
        //             return;
        //         }

        //         foreach (var guid in guids)
        //         {
        //             string path = AssetDatabase.GUIDToAssetPath(guid);
        //             Debug.Log($"Found XRGeneralSettings asset at: {path}");
        //         }
        //     }
        // }

        private static void EnableOpenXRTouchSupport(BuildTargetGroup buildTargetGroup)
        {
            // Enable Oculus Touch controller profile
            OpenXRFeature oculusFeature = GetOrCreateFeature<OculusTouchControllerProfile>(buildTargetGroup);

            if (oculusFeature != null && !oculusFeature.enabled)
            {
                oculusFeature.enabled = true;
                Debug.Log("✅ Oculus Touch Controller Profile enabled.");
            }
            else if (oculusFeature != null)
            {
                Debug.Log("ℹ️ Oculus Touch Controller Profile was already enabled.");
            }
            else
            {
                Debug.LogWarning("⚠️ Could not find OculusTouchControllerProfile.");
            }

            AssetDatabase.SaveAssets();
        }

        private static T GetOrCreateFeature<T>(BuildTargetGroup group) where T : OpenXRFeature
        {
            var settings = OpenXRSettings.GetSettingsForBuildTargetGroup(group);
            if (settings == null) return null;

            return settings.GetFeatures().OfType<T>().FirstOrDefault();
        }
    }
}
#endif
