#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;
using VE2.Common.Shared;
using VE2.Core.Player.Internal;

namespace VE2.Player.Internal 
{
    internal class PlayerEditorMenu
    {
        [MenuItem("/GameObject/VE2/PlayerSpawner", priority = 1)]
        private static void CreatePlayerSpawner()
        {
            V_PlayerSpawner playerSpawner = GameObject.FindFirstObjectByType<V_PlayerSpawner>();

            if (playerSpawner == null)
                CommonUtils.InstantiateResource("PlayerSpawner");
            else
                EditorUtility.DisplayDialog("Error", "Looks like a PlayerSpawner already exists in your scene - you can only have one!", "Ok");
        }

        [MenuItem("/GameObject/VE2/TeleportAnchor", priority = 10)]
        private static void CreateTeleportAnchor()
        {
            CommonUtils.InstantiateResource("TeleportAnchor");
        }
    }
}
#endif
