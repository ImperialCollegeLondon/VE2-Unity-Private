using UnityEngine;
using VE2.Common.API;
using VE2.NonCore.FileSystem.API;
using VE2.NonCore.Platform.API;
using static VE2.NonCore.Platform.API.PlatformPublicSerializables;

//TODO: If user tries to upload a world that already exists but under a different category, don't let them 
//TODO: Think about how this actually works in the hub. Maybe the hub controller should listen for platform ready, and then turn on the filesys?
//Then again ^ if there's no FTP settings, we still want the filesys to work, just offline
//We'll need to rethink it a tad, allow the filesystem to work offline, and just talk to the local dir

namespace VE2.NonCore.FileSystem.Internal
{
    internal class InternalFileSystem : FileSystemIntegrationBase, IFileSystemInternal
    {
        public override string RemoteWorkingPath{ get {
               string platformName = Application.platform == RuntimePlatform.Android ? "Android" : "Windows";

               return $"VE2/Worlds/{platformName}";
            }
        }

        private void OnEnable()
        {
            if (VE2API.PlatformService == null)
            {
                Debug.LogError("Can't boot file system, no platform service found.");
                return;
            }

            if (VE2API.PlatformService.IsConnectedToServer)
                HandlePlatformReady();
            else
                VE2API.PlatformService.OnConnectedToServer += HandlePlatformReady;
        }

        private void HandlePlatformReady()
        {
            ServerConnectionSettings serverSettings = ((IPlatformServiceInternal)VE2API.PlatformService).GetInternalWorldStoreFTPSettings();

            if (serverSettings == null)
            {
                Debug.LogError("Can't boot internal file system, no server settings returned from platform");
                return;
            }

            CreateFileSystem(serverSettings);
        }

        private void OnDisable()
        {
            if (!Application.isPlaying)
                return;

            _FileStorageService?.TearDown();
            _FileStorageService = null;
        }
    }
}
