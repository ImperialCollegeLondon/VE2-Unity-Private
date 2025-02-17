using System;
using System.Collections.Generic;
using static VE2.Platform.API.PlatformPublicSerializables;

internal interface IPlatformServiceInternal : IPlatformService
{
    //public List<(string, int)> ActiveWorldsNamesAndVersions { get; }
    public ushort LocalClientID { get; }
    public bool IsAuthFailed { get; }
    public event Action OnAuthFailed;
    public GlobalInfo GlobalInfo { get; }
    public event Action<GlobalInfo> OnGlobalInfoChanged;

    public Dictionary<string, WorldDetails> ActiveWorlds { get; }

    public void RequestInstanceAllocation(string worldName, string instanceSuffix);

    public ServerConnectionSettings GetInstanceServerSettingsForWorld(string worldName);

    public ServerConnectionSettings GetInstanceServerSettingsForCurrentWorld();

}

/*
 *  One interface that faces the platform integration package that gets imported by customers 
 *  Another interface that faces the private platform stuff, the same package that the PlatformService lives in, is meant to provide available worlds, and global info 
 * 
 */