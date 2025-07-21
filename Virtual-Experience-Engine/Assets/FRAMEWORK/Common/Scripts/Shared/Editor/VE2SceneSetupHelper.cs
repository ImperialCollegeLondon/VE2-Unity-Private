#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace VE2.Common.Shared
{
    internal class VE2SceneSetupHelper
    {
        private static string _sceneName => "MyVE2QuickStartScene";

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

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            Debug.Log("VE2 scene setup complete.");

            // Highlight the new scene asset in the Project window
            var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath);
            if (sceneAsset != null)
            {
                Selection.activeObject = sceneAsset;
                EditorGUIUtility.PingObject(sceneAsset);
            }

            int result = EditorUtility.DisplayDialogComplex(
                "VE2 Quickstart Scene Setup Complete",
                "A quickstart scene has been created and opened in the editor, ready for you to begin using VE2.\n\n" +
                "⚠️ Remember to RENAME THIS SCENE to something unique before exporting it to the VE2 platform.",
                "Got it!",
                "Show me docs for this scene",
                null
            );

            if (result == 1) // "Show me docs for this scene"
            {
                Application.OpenURL("https://www.notion.so/Understanding-the-VE2-Quickstart-Scene-2290e4d8ed4d80c4a0dcceda148f51ef"); 
            }
        }
    }
}

#endif
