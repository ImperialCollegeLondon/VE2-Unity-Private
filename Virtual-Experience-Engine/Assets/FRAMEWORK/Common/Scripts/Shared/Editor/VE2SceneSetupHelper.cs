#if UNITY_EDITOR
using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features;
using UnityEngine.XR.OpenXR.Features.Interactions;

namespace VE2.Common.Shared
{
    internal class VE2SceneSetupHelper
    {
        private static string _sceneName => "MyVE2QuickStartScene";

        [MenuItem("VE2/Create Quick Start Scene", priority = -997)]
        internal static void CreateQuickStartScene()
        {
            // Ensure Scenes folder exists
            string scenesFolder = "Assets/Scenes";
            if (!AssetDatabase.IsValidFolder(scenesFolder))
                AssetDatabase.CreateFolder("Assets", "Scenes");

            // Make scene name unique
            string safeSceneName = _sceneName.Trim();
            if (string.IsNullOrWhiteSpace(safeSceneName))
                safeSceneName = "VE2Scene";

            string scenePath = $"{scenesFolder}/{safeSceneName}.unity";
            scenePath = AssetDatabase.GenerateUniqueAssetPath(scenePath);

            // Create new scene and save it
            var newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EditorSceneManager.SaveScene(newScene, scenePath);
            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            Debug.Log($"Created and opened new scene at {scenePath}");

            // Load and instantiate the VE2 setup prefab
            GameObject sceneSetupPrefab = Resources.Load<GameObject>("VE2SetupSceneHolder");
            if (sceneSetupPrefab == null)
            {
                Debug.LogError("Could not find VE2SetupSceneHolder in Resources.");
                return;
            }

            GameObject instantiatedSceneSetup = GameObject.Instantiate(sceneSetupPrefab);

            // Unpack children of holder
            for (int i = instantiatedSceneSetup.transform.childCount - 1; i >= 0; i--)
            {
                Transform child = instantiatedSceneSetup.transform.GetChild(i);
                child.SetParent(null);
            }

            GameObject.DestroyImmediate(instantiatedSceneSetup);
            Debug.Log("VE2 scene setup complete.");

            // Highlight the new scene asset in the Project window
            var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath);
            if (sceneAsset != null)
            {
                Selection.activeObject = sceneAsset;
                EditorGUIUtility.PingObject(sceneAsset);
            }

            // Show info popup
            EditorUtility.DisplayDialog(
                "VE2 Quickstart Scene Created",
                "A quickstart scene has been created and opened in the editor, ready for you to begin using VE2.\n\n" +
                "⚠️ Remember to RENAME THIS SCENE to something unique before exporting it to the VE2 platform.",
                "Got it!"
            );
        }

        [MenuItem("VE2/Enable OpenXR + Oculus Touch Profile")]
        private static void EnableOpenXRTouchSupport()
        {
            BuildTargetGroup buildTargetGroup = BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget);

            // Enable OpenXR loader in XR Plugin Management
            XRSetupUtility.EnableOpenXRLoader(buildTargetGroup);

            // Enable Oculus Touch controller profile
            OpenXRFeature oculusFeature = OpenXRFeatureHelpers.GetOrCreateFeature<OculusTouchControllerProfile>(buildTargetGroup);
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


public static class XRSetupUtility
{
    private const string GeneralSettingsPath = "Assets/XR/XRGeneralSettingsPerBuildTarget.asset";
    private const string ManagerSettingsAssetPath = "Assets/XRManagerSettings.asset";

    public static void EnableOpenXRLoader(BuildTargetGroup buildTargetGroup)
    {
        var generalSettingsAsset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(GeneralSettingsPath);
        if (generalSettingsAsset == null)
        {
            Debug.LogError($"Failed to load XRGeneralSettingsPerBuildTarget asset at '{GeneralSettingsPath}'");
            return;
        }

        var serializedSettings = new SerializedObject(generalSettingsAsset);
        var keysProp = serializedSettings.FindProperty("Keys");
        var valuesProp = serializedSettings.FindProperty("Values");

        if (keysProp == null || valuesProp == null)
        {
            Debug.LogError("Could not find 'Keys' or 'Values' properties in XRGeneralSettingsPerBuildTarget asset.");
            return;
        }

        // Find index of the target build target group in the Keys array
        int foundIndex = -1;
        for (int i = 0; i < keysProp.arraySize; i++)
        {
            var keyProp = keysProp.GetArrayElementAtIndex(i);
            var keyEnumIndex = keyProp.enumValueIndex;
            if ((BuildTargetGroup)keyEnumIndex == buildTargetGroup)
            {
                foundIndex = i;
                break;
            }
        }

        if (foundIndex == -1)
        {
            // Add new entry for this build target group
            foundIndex = keysProp.arraySize;
            keysProp.InsertArrayElementAtIndex(foundIndex);
            valuesProp.InsertArrayElementAtIndex(foundIndex);

            var newKeyProp = keysProp.GetArrayElementAtIndex(foundIndex);
            var newValueProp = valuesProp.GetArrayElementAtIndex(foundIndex);

            newKeyProp.enumValueIndex = (int)buildTargetGroup;
            newValueProp.objectReferenceValue = null;

            serializedSettings.ApplyModifiedProperties();
            Debug.Log($"Added new build target group entry for {buildTargetGroup}");
        }

        var assignedSettingsProp = valuesProp.GetArrayElementAtIndex(foundIndex);
        XRManagerSettings managerSettings = assignedSettingsProp.objectReferenceValue as XRManagerSettings;

        // If no manager assigned, create one
        if (managerSettings == null)
        {
            managerSettings = ScriptableObject.CreateInstance<XRManagerSettings>();
            AssetDatabase.CreateAsset(managerSettings, ManagerSettingsAssetPath);
            AssetDatabase.SaveAssets();

            assignedSettingsProp.objectReferenceValue = managerSettings;
            serializedSettings.ApplyModifiedProperties();

            Debug.Log("Created and assigned new XRManagerSettings asset.");
        }
        else
        {
            Debug.Log("XRManagerSettings already assigned.");
        }

        // Add OpenXRLoader if not present
        bool loaderAdded = false;
        foreach (var loader in managerSettings.loaders)
        {
            if (loader is OpenXRLoader)
            {
                loaderAdded = true;
                break;
            }
        }

        if (!loaderAdded)
        {
            var openXRLoaderInstance = ScriptableObject.CreateInstance<OpenXRLoader>();
            if (managerSettings.TryAddLoader(openXRLoaderInstance))
            {
                AssetDatabase.SaveAssets();
                Debug.Log("✅ OpenXR loader successfully added.");
            }
            else
            {
                Debug.LogWarning("Failed to add OpenXR loader to XRManagerSettings.");
            }
        }
        else
        {
            Debug.Log("ℹ️ OpenXR loader was already enabled.");
        }
    }
}




public class FindXRGeneralSettingsAsset
{
    [MenuItem("VE2/Find XRGeneralSettings Asset")]
    private static void FindSettingsAsset()
    {
        string[] guids = AssetDatabase.FindAssets("t:XRGeneralSettings");
        if (guids.Length == 0)
        {
            Debug.LogWarning("No XRGeneralSettings asset found in project.");
            return;
        }

        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Debug.Log($"Found XRGeneralSettings asset at: {path}");
        }
    }
}


        internal static class OpenXRFeatureHelpers
        {
            public static T GetOrCreateFeature<T>(BuildTargetGroup group) where T : OpenXRFeature
            {
                var settings = OpenXRSettings.GetSettingsForBuildTargetGroup(group);
                if (settings == null) return null;

                return settings.GetFeatures().OfType<T>().FirstOrDefault();
            }
        }
    }
}
#endif
