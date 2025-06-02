#if UNITY_EDITOR

using UnityEditor;

namespace VE2.NonCore.FileSystem.Internal 
{
    internal class VComponentsEditorMenu
    {
        [MenuItem("/GameObject/VE2/Networking/FileSystem", priority = 100)]
        private static void CreateFileSystem()
        {
            VE2.Common.Shared.CommonUtils.InstantiateResource("FileSystem");
        }
    }
}
#endif