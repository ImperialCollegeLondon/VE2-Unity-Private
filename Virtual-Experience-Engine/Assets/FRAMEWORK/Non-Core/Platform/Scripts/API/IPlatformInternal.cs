using System;
using System.Collections.Generic;
using static VE2.NonCore.Platform.API.PlatformPublicSerializables;

namespace VE2.NonCore.Platform.API
{
    internal interface IPlatformServiceInternal : IPlatformService //TODO: Do we even need plugin-facing APIs here?
    {
        public void UpdateSettings(ServerConnectionSettings serverConnectionSettings, InstanceCode instanceCode);
        public InstanceCode CurrentInstanceCode { get; }
        public Dictionary<InstanceCode, PlatformInstanceInfo> InstanceInfos { get; }
        public event Action<Dictionary<InstanceCode, PlatformInstanceInfo>> OnInstanceInfosChanged;

        public void ConnectToPlatform();

        public string PlayerDisplayName { get; }

        public ushort LocalClientID { get; }
        public bool IsAuthFailed { get; }
        public event Action OnAuthFailed;

        //May not need these, the platform UI will be part of Platform.Internal anyway
        // public GlobalInfo GlobalInfo { get; }
        // public event Action<GlobalInfo> OnGlobalInfoChanged;
        // public Dictionary<string, WorldDetails> ActiveWorlds { get; }

        public List<(string, int)> ActiveWorldsNamesAndVersions { get; }
        public void RequestInstanceAllocation(InstanceCode instanceCode);
        public void ReturnToHub();

        public ServerConnectionSettings GetInstanceServerSettingsForWorld(string worldName);

        public ServerConnectionSettings GetInstanceServerSettingsForCurrentWorld();

        public ServerConnectionSettings GetWorldSubStoreFTPSettingsForCurrentWorld();

        public ServerConnectionSettings GetInternalWorldStoreFTPSettings();

        public void TearDown();
    }
}
