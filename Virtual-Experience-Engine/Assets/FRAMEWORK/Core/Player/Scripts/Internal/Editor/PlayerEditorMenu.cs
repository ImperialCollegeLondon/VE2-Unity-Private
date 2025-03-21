#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;
using VE2.Core.Player.Internal;

namespace VE2.Player.Internal 
{
    public class PlayerEditorMenu
    {
        [MenuItem("/GameObject/VE2/PlayerSpawner", priority = 1)]
        private static void CreateViRSEManager()
        {
            V_PlayerSpawner playerSpawner = GameObject.FindFirstObjectByType<V_PlayerSpawner>();

            if (playerSpawner == null)
            {
                GameObject playerSpawnerGO = GameObject.Instantiate(Resources.Load<GameObject>("PlayerSpawner"));
                Selection.activeGameObject = playerSpawnerGO;
                playerSpawnerGO.transform.position = Vector3.zero;
                playerSpawnerGO.transform.rotation = Quaternion.identity;
                playerSpawnerGO.name = "PlayerSpawner";

                // Add the instantiation to the Undo buffer
                Undo.RegisterCreatedObjectUndo(playerSpawnerGO, "Create " + playerSpawnerGO.name);
            }
            else
            {
                EditorUtility.DisplayDialog("Error", "Looks like a PlayerSpawner already exists in your scene - you can only have one!", "Ok");
            }
        }
    }
}
#endif
