#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;
using VE2.Common.Shared;

namespace VE2.Core.VComponents.Internal 
{
    internal class CommonEditorMenu
    {
        [MenuItem("/GameObject/VE2/UIs/WorldSpaceUI", priority = 0)]
        private static void CreateCustomInfoPoint()
        {
            CommonUtils.InstantiateResource("WorldspaceUI");
        }
    }
}
#endif