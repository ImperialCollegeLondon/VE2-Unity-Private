using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class DebugPlayerThings : MonoBehaviour
{
    void Update()
    {
        if (Keyboard.current.tKey.wasPressedThisFrame)
            EditorUtility.RequestScriptReload();
    }
}
