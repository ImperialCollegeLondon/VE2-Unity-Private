#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace VE2.Core.Player.Internal
{
    internal class DebugPlayerThings : MonoBehaviour
    {
        void Update()
        {
            if (Keyboard.current.tKey.wasPressedThisFrame)
                EditorUtility.RequestScriptReload();
        }
    }
}

#endif
