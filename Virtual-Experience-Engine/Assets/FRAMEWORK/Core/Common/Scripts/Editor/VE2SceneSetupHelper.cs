#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace VE2.Core.Common
{
    internal class VE2SceneSetupHelper
    {
        [MenuItem("VE2/Setup Scene", priority = 1)]
        internal static void ShowWindow()
        {
            // Create a new window instance
            var window = ScriptableObject.CreateInstance<VE2SceneSetupWindow>();
            window.position = new Rect(Screen.width / 2, Screen.height / 2, 200, 100);
            window.titleContent = new GUIContent("VE2 Scene Setup");
            
            // Show the window
            window.Show();
        }
    }

    internal class VE2SceneSetupWindow : EditorWindow
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
            GameObject sceneSetupPrefab = Resources.Load<GameObject>("VE2SetupSceneHolder");
            if (sceneSetupPrefab == null)
            {
                Debug.LogError("Could not find scene setup resource");
                return;
            }

            // Destroy all GameObjects in the scene
            foreach (var go in GameObject.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                GameObject.DestroyImmediate(go);

            // Instantiate the scene setup prefab, remove the holder
            GameObject instantiatedSceneSetup = GameObject.Instantiate(sceneSetupPrefab);

            // Iterate backward to avoid skipping elements
            for (int i = instantiatedSceneSetup.transform.childCount - 1; i >= 0; i--)
            {
                Transform child = instantiatedSceneSetup.transform.GetChild(i);
                child.SetParent(null);
            }

            //No longer need the holder, destroy it
            DestroyImmediate(instantiatedSceneSetup);
        }
    }
}
#endif
