#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;
using VE2.Core.Common;

namespace VE2.NonCore.FileSystem.Internal 
{
    public class VComponentsEditorMenu
    {
        [MenuItem("/GameObject/VE2/Networking/FileSystem", priority = 100)]
        private static void CreateFileSystem()
        {
            CommonUtils.InstantiateResource("FileSystem");
        }
    }
}
#endif