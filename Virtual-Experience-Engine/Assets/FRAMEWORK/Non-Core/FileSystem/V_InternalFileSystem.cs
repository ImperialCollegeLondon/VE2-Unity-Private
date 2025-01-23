using UnityEngine;
using UnityEngine.PlayerLoop;
using VE2_NonCore_FileSystem_Interfaces_Internal;

namespace VE2_NonCore_FileSystem
{
    public class V_InternalFileSystem : V_FileSystemIntegrationBase, IInternalFileSystem
    {
        public override string LocalWorkingPath{ get {
            EnvironmentConfig environmentConfig = Resources.Load<EnvironmentConfig>("EnvironmentConfig");
               return $"VE2/Worlds/{environmentConfig.EnvironmentName}";
            }
        }
    }
}

//TODO: If user tries to upload a world that already exists but under a different category, don't let them 
//Need to remember all world folders, strip their category, and make sure our world name doesn't match