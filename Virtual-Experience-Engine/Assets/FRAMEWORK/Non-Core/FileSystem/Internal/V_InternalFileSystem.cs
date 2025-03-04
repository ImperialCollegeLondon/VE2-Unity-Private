using UnityEngine;
using VE2.NonCore.FileSystem.API;

namespace VE2.NonCore.FileSystem.Internal
{
    internal class V_InternalFileSystem : V_FileSystemIntegrationBase, IFileSystemInternal
    {
        public override string LocalWorkingPath{ get {
               string platformName = Application.platform == RuntimePlatform.Android ? "Android" : "Windows";

               return $"VE2/Worlds/{platformName}";
            }
        }
    }
}

//TODO: If user tries to upload a world that already exists but under a different category, don't let them 
//Need to remember all world folders, strip their category, and make sure our world name doesn't match