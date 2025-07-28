using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace VE2.Common.Shared
{
    internal static class HiddenGORevealerMenu
    {
#if UNITY_EDITOR
        [MenuItem("VE2/Misc/Reveal Hidden GameObjects")]
        private static void RevealHiddenGameObjects()
        {
            // Find all GameObjects in the current scenes, including inactive and hidden
            GameObject[] allGameObjects = Object.FindObjectsOfType<GameObject>(true);

            List<GameObject> hiddenGameObjects = new();

            foreach (GameObject go in allGameObjects)
            {
                if (go.hideFlags != HideFlags.None)
                {
                    go.hideFlags = HideFlags.None;
                    hiddenGameObjects.Add(go);
                }
            }

            // Refresh the hierarchy view so changes become visible
            EditorApplication.RepaintHierarchyWindow();

            Debug.Log($"Revealed {hiddenGameObjects.Count} hidden GameObject(s).");
            if (hiddenGameObjects.Count > 0)
            {
                Debug.Log("Hidden GameObjects revealed: " + string.Join(", ", hiddenGameObjects.ConvertAll(go => go.name)));
            }
            else
            {
                Debug.Log("No hidden GameObjects found.");
            }
        }
#endif
    }
}