using UnityEngine;
using UnityEngine.SceneManagement;
using VE2.NonCore.FileSystem.API;
using VE2.NonCore.Platform.API;
using static VE2.NonCore.Platform.API.PlatformPublicSerializables;

namespace VE2.NonCore.FileSystem.Internal
{
    internal class V_PluginFileSystem : FileSystemIntegrationBase, IFileSystem
    {
        [Title("Debug Server Settings")]
        [BeginGroup, IgnoreParent, EndGroup, SerializeField] private ServerConnectionSettings _debugServerSettings;
    [   EditorButton(nameof(OpenLocalWorkingFolder), "Open Local Working Folder", activityType: ButtonActivityType.Everything)]
        [SerializeField, DisableInPlayMode, SpaceArea(spaceAfter: 5, Order = 50)] private bool _useDebugSettingsInBuild = false;

        public override string LocalWorkingPath => $"VE2/PluginFiles/{SceneManager.GetActiveScene().name}";

        private void OnEnable()
        {
            ServerConnectionSettings serverSettings = ((IPlatformServiceInternal)PlatformAPI.PlatformService).GetWorldSubStoreFTPSettingsForCurrentWorld();

            if (serverSettings == null)
            {
                if (Application.isEditor || _useDebugSettingsInBuild)
                {
                    serverSettings = _debugServerSettings;
                }
                else
                {
                    Debug.LogError("Can't boot file system, no server settings returned from platform, and debug settings are disabled in build");
                    return;
                }   
            }

            CreateFileSystem(serverSettings);
        }
    }
}
