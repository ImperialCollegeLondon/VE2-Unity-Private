#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace VE2.Core.Player.Internal
{
    [AddComponentMenu("")] // Prevents this MonoBehaviour from showing in the Add Component menu
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
