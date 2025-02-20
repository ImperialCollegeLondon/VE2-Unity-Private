using System;
using System.Collections.Generic;
using static VE2.Platform.API.PlatformPublicSerializables;

namespace VE2.NonCore.Platform.API
{
    internal interface IPlatformServiceInternal : IPlatformService
    {
        public ushort LocalClientID { get; }
        public bool IsAuthFailed { get; }
        public event Action OnAuthFailed;

        //May not need these, the platform UI will be part of Platform.Internal anyway
        // public GlobalInfo GlobalInfo { get; }
        // public event Action<GlobalInfo> OnGlobalInfoChanged;
        // public Dictionary<string, WorldDetails> ActiveWorlds { get; }

        public List<(string, int)> ActiveWorldsNamesAndVersions { get; }
        public void RequestInstanceAllocation(string worldName, string instanceSuffix);

        public ServerConnectionSettings GetInstanceServerSettingsForWorld(string worldName);

        public ServerConnectionSettings GetInstanceServerSettingsForCurrentWorld();

    }
}
