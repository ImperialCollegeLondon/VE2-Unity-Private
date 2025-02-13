using System;
using System.Collections.Generic;
using static VE2.Platform.API.PlatformPublicSerializables;

internal interface IPlatformServiceInternal : IPlatformService
{
    //public List<(string, int)> ActiveWorldsNamesAndVersions { get; }
    public Dictionary<string, WorldDetails> ActiveWorlds { get; }

    public void RequestInstanceAllocation(string worldName, string instanceSuffix);

    internal ServerConnectionSettings GetInstanceServerSettingsForWorld(string worldName);

    internal ServerConnectionSettings GetInstanceServerSettingsForCurrentWorld();
}

/*
 *  One interface that faces the platform integration package that gets imported by customers 
 *  Another interface that faces the private platform stuff, the same package that the PlatformService lives in, is meant to provide available worlds, and global info 
 * 
 */