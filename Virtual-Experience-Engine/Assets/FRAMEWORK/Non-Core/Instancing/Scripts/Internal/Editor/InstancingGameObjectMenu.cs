#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;
using VE2.Common.Shared;
using VE2.NonCore.Instancing.Internal;

namespace VE2.NonCore.FileSystem.Internal 
{
    internal class VComponentsEditorMenu
    {
        [MenuItem("/GameObject/VE2/Networking/NetworkedGrabbable", priority = 97)]
        private static void CreateNetworkedGrabbable()
        {

            CommonUtils.InstantiateResource("NetworkedGrabbable");
        }

        [MenuItem("/GameObject/VE2/Networking/InstanceIntegration", priority = 98)]
        private static void CreateInstanceIntegration()
        {
            V_InstanceIntegration instanceIntegration = GameObject.FindFirstObjectByType<V_InstanceIntegration>();

            if (instanceIntegration != null)
            {
                Debug.LogError("InstanceIntegration already exists in the scene");
                Selection.activeObject = instanceIntegration.gameObject;
                return;
            }

            CommonUtils.InstantiateResource("InstanceIntegration");
        }
    }
}
#endif