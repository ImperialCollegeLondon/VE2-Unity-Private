using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class DebugPlayerThings : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Keyboard.current.rKey.wasPressedThisFrame)
            EditorUtility.RequestScriptReload();

        if (Keyboard.current.pKey.wasPressedThisFrame)
        {

            Debug.Log("PLAYER Going to dev scene"); //Logs out fine 
            Debug.Log($"PLAYER Scene count: {SceneManager.sceneCount}"); //Fails silently, doesn't log
            Debug.Log($"PLAYER Scene count in build settings: {SceneManager.sceneCountInBuildSettings}"); //Fails silently, doesn't log
            int sceneCount = SceneManager.sceneCountInBuildSettings;
            for (int i = 0; i < sceneCount; i++)
            {
                string scenePath = SceneUtility.GetScenePathByBuildIndex(i);
                string sceneName = System.IO.Path.GetFileNameWithoutExtension(scenePath);
                Debug.Log("PLAYER Scene " + i + ": " + sceneName);
            }
        }
    }
}
