using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

[ExecuteInEditMode]
public class V_PlatformIntegration : MonoBehaviour, IInstanceNetworkSettingsProvider, IPlayerSettingsProvider
{
    //TODO, only show these if there actually is a player, and if these things are actually in the scene
    [Help("For mocking the network settings that will be sent to V_InstanceIntegration in the editor. When you export your built world to the platform, the platform will override these settings.")]
    [SerializeField] private InstanceNetworkSettings DebugInstanceNetworkSettings = new("127.0.0.1", 4297, "dev");

    [Help("For mocking the player settings that will be sent to V_PlayerSpawner in the editor. When you export your built world to the platform, the platform will override these settings.")]
    [SerializeField] private UserSettings DebugPlayerSettings = new();

    private IPlatformService _platformService;
    public IPlatformService PlatformService {
        get {
            if (_platformService == null)
                OnEnable();

            return _platformService;
        }
    }

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

    #region Shared interfaces 
    public string GameObjectName => gameObject.name;
    public bool IsEnabled => enabled && gameObject.activeInHierarchy;
    #endregion


    private void OnEnable() //Called on reconnection
    {
        if (!Application.isPlaying)
        {
            ViRSENonCoreServiceLocator.Instance.InstanceNetworkSettingsProvider = this;
            ViRSECoreServiceLocator.Instance.PlayerSettingsProvider = this;
            return;
        }

        GameObject platformProviderGO = GameObject.Find("PlatformServiceProvider");

        if (platformProviderGO != null)
        {
            _platformService = platformProviderGO.GetComponent<IPlatformServiceProvider>().PlatformService;
        }
        else
        {
            _platformService = new DebugPlatformService(SceneManager.GetActiveScene().name + "-debug");
            InstanceNetworkSettings debugNetworkSettings = _platformService.InstanceNetworkSettings;
            Debug.LogWarning($"No platform service provider found, using debug platform service. " +
                $"This will return default user settings, and the following instance networking settings" +
                $"IP: {debugNetworkSettings.IP}, Port: {debugNetworkSettings.Port} Instance code: {debugNetworkSettings.InstanceCode}");
        }

        //if (_platformService.IsConnectedToServer)
        //    HandlePlatformServiceReady();
        //else
        //    _platformService.OnConnectedToServer += HandlePlatformServiceReady;
    }

    //private void 

    //private void HandlePlatformServiceReady()
    //{
    //    GameObject playerSpawnerGO = GameObject.Find("VirsePlayer"); TODO player stuff

    //    GameObject instanceIntegrationGO = GameObject.Find("PluginSyncer");
    //    if (instanceIntegrationGO != null)
    //    {
    //        IInstanceNetworkSettingsReceiver instanceSettingsReceiver = instanceIntegrationGO.GetComponent<IInstanceNetworkSettingsReceiver>();
    //        if (instanceSettingsReceiver != null)
    //        {
    //            instanceSettingsReceiver.SetInstanceNetworkSettings(_platformService.InstanceNetworkSettings);
    //        }
    //    }
    //}

    private void OnDisable()
    {
        if (!Application.isPlaying)
        {
            //ViRSENonCoreServiceLocator.Instance.InstanceNetworkSettingsProvider = null;
            return;
        }

        //if (_platformService != null)
        //    _platformService.OnConnectedToServer -= HandlePlatformServiceReady;
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

/*


*/
