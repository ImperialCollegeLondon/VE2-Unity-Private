#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;
using VE2.Core.Common;
using VE2.NonCore.Platform.Internal;

namespace VE2.NonCore.FileSystem.Internal 
{
    public class VComponentsEditorMenu
    {
        [MenuItem("/GameObject/VE2/Networking/PlatformIntegration", priority = 99)]
        private static void CreatePlatformIntegration()
        {
            V_PlatformIntegration platformIntegration = GameObject.FindFirstObjectByType<V_PlatformIntegration>();

            if (platformIntegration != null)
            {
                Debug.LogError("PlatformIntegration already exists in the scene");
                Selection.activeObject = platformIntegration.gameObject;
                return;
            }

            CommonUtils.InstantiateResource("PlatformIntegration");
        }
    }
}
#endif