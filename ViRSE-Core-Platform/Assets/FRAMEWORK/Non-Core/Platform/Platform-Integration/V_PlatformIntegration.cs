using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class V_PlatformIntegration : MonoBehaviour, IInstanceNetworkSettingsProvider, IPlayerSettingsProvider
{
    #region Instance-Networking-Facing Interfaces
    public bool AreInstanceNetworkingSettingsReady => PlatformService.IsConnectedToServer;
    public event Action OnInstanceNetworkSettingsReady { add { PlatformService.OnConnectedToServer += value; } remove { PlatformService.OnConnectedToServer -= value; } }
    public InstanceNetworkSettings InstanceNetworkSettings => PlatformService.InstanceNetworkSettings;
    #endregion

    #region Player-Rig-Facing Interfaces
    public bool ArePlayerSettingsReady => PlatformService.IsConnectedToServer;
    public event Action OnPlayerSettingsReady { add { PlatformService.OnConnectedToServer += value; } remove { PlatformService.OnConnectedToServer -= value; } }
    public UserSettings UserSettings => PlatformService.UserSettings;
    #endregion


    private IPlatformService _platformService;
    private IPlatformService PlatformService
    {
        get
        {
            if (_platformService != null)
                return _platformService;
            else
                Awake();

            return _platformService;
        }
    }

    private IPlatformServiceProvider _platformServiceProvider 
    { 
        get 
        {
            GameObject plataformProviderGO = GameObject.Find("PlatformServiceProvider");
            if (plataformProviderGO == null)
                return null;

            return plataformProviderGO.GetComponent<IPlatformServiceProvider>();
        } 
    }

    private void Awake()
    {
        //Debug.Log("Platform integration awake - " + (_platformServiceProvider != null));

        if (_platformServiceProvider != null)
        {
            _platformService = _platformServiceProvider.PlatformService;
        }
        else
        {
            _platformService = new DebugPlatformService();
            InstanceNetworkSettings debugNetworkSettings = _platformService.InstanceNetworkSettings;
            Debug.LogWarning($"No platform service provider found, using debug platform service. " +
                $"This will return default user settings, and the following instance networking settings" +
                $"IP: {debugNetworkSettings.IP}, Port: {debugNetworkSettings.Port} Instance code: {debugNetworkSettings.InstanceCode}");
        }
    }
}

public class DebugPlatformService : IPlatformService
{
    public bool IsConnectedToServer => true;

    public UserSettings UserSettings => new();

    public InstanceNetworkSettings InstanceNetworkSettings => new InstanceNetworkSettings("127.0.0.1", 4296, "debug");

    public event Action OnConnectedToServer;

    public void RequestInstanceAllocation(string worldName, string instanceSuffix)
    {
        Debug.Log($"Request instance allocation to {worldName}-{instanceSuffix} from debug platform service");
    }

    public void TearDown()
    {
        Debug.Log("Tear down debug platform service");
    }
}
