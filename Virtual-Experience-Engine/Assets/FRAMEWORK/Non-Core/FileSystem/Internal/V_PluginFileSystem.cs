using UnityEngine.SceneManagement;
using VE2.NonCore.FileSystem.API;

namespace VE2.NonCore.FileSystem.Internal
{
    internal class V_PluginFileSystem : V_FileSystemIntegrationBase, IFileSystem
    {
        public override string LocalWorkingPath => $"VE2/PluginFiles/{SceneManager.GetActiveScene().name}";
    }
}
