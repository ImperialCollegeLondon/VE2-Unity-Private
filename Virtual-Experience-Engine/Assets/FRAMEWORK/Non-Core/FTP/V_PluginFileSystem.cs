using UnityEngine.SceneManagement;
using VE2_NonCore_FileSystem_Interfaces_Plugin;

namespace VE2_NonCore_FileSystem
{
    public class V_PluginFileSystem : V_FileSystemIntegrationBase, IPluginFileSystem
    {
        public override string LocalWorkingPath => $"VE2/PluginFiles/{SceneManager.GetActiveScene().name}";
    }
}
