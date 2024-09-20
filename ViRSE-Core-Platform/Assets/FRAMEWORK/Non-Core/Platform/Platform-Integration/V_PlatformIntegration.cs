using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class V_PlatformIntegration : MonoBehaviour
{
    [Help("These settings allow you to test in the editor, when you export your world to the platform, the platform will override these settings.")]
    [SerializeField] private InstanceNetworkSettings DebugInstanceNetworkSettings = new("127.0.0.1", 4297, "dev");

    private IPlatformService _platformService;

    private void OnEnable()
    {
        GameObject plataformProviderGO = GameObject.Find("PlatformServiceProvider");

        if (plataformProviderGO != null)
        {
            _platformService = plataformProviderGO.GetComponent<IPlatformServiceProvider>().PlatformService;
        }
        else
        {
            _platformService = new DebugPlatformService(SceneManager.GetActiveScene().name + "-debug");
            InstanceNetworkSettings debugNetworkSettings = _platformService.InstanceNetworkSettings;
            Debug.LogWarning($"No platform service provider found, using debug platform service. " +
                $"This will return default user settings, and the following instance networking settings" +
                $"IP: {debugNetworkSettings.IP}, Port: {debugNetworkSettings.Port} Instance code: {debugNetworkSettings.InstanceCode}");
        }

        if (_platformService.IsConnectedToServer)
            HandlePlatformServiceReady();
        else
            _platformService.OnConnectedToServer += HandlePlatformServiceReady;
    }

    private void HandlePlatformServiceReady()
    {
        //GameObject playerSpawnerGO = GameObject.Find("VirsePlayer"); TODO player stuff 

        GameObject instanceIntegrationGO = GameObject.Find("PluginSyncer");
        if (instanceIntegrationGO != null)
        {
            IInstanceNetworkSettingsReceiver instanceSettingsReceiver = instanceIntegrationGO.GetComponent<IInstanceNetworkSettingsReceiver>();
            if (instanceSettingsReceiver != null)
            {
                instanceSettingsReceiver.SetInstanceNetworkSettings(_platformService.InstanceNetworkSettings);
            }
        }
    }

    private void OnDisable()
    {
        if (_platformService != null)
            _platformService.OnConnectedToServer -= HandlePlatformServiceReady;
    }
}

public class DebugPlatformService : IPlatformService
{
    public bool IsConnectedToServer => true;

    public UserSettings UserSettings => new();

    public InstanceNetworkSettings InstanceNetworkSettings { get; private set; }

    public event Action OnConnectedToServer;

    public DebugPlatformService(string instanceCode)
    {
        InstanceNetworkSettings = new InstanceNetworkSettings("127.0.0.1", 4297, instanceCode);
    }

    public void RequestInstanceAllocation(string worldName, string instanceSuffix)
    {
        Debug.Log($"Request instance allocation to {worldName}-{instanceSuffix} from debug platform service");
    }

    public void TearDown()
    {
        Debug.Log("Tear down debug platform service");
    }
}
