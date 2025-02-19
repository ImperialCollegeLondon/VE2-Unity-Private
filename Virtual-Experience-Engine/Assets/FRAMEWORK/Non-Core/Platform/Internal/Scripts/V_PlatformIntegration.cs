using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using VE2.NonCore.Platform.Private;
using static VE2.Platform.API.PlatformPublicSerializables;

namespace VE2.PlatformNetworking
{

    /*
        Should we have an interface for this?
        The hub UI needs it, and the player browser needs it...
        Do we really want to ship all this code to windows customers? Seems unnecessary
        This probably _should_ go through the service locator. V_PlatformIntegration should implement IPlatformIntegration 
        V_PlatformIntegration, on edit-mode awake, should set the service locator's IPlatformIntegration to itself
        Then, when anything needs to talk to the platform, it should go through the service locator
        This means we don't need ship the hub code to the customer - the hub code should be a different assembly to everything else 


        ALSO==================

        The monobehaviour is effectively the factory 
        All it really needs is a lazy init getter for the service 
        that would then mean this needs another interface altogether, IE PlatformServiceProvider
        So, locator implements IPlatformService, its implementation points towards the IPlatformServiceProvider

        The alternative is the Locator, and the integration, and the 
    */

    [ExecuteInEditMode]
    internal class V_PlatformIntegration : MonoBehaviour, IPlatformProvider //, IPlatformService, IPlatformServiceInternal
    {
        #region Debug settings
        [Help("If starting in this scene rather than the Hub (e.g, when testing in the editor), these settings will be used.")]
        [BeginGroup("Debug settings"), SerializeField] private bool OfflineMode = false;
        [SerializeField, HideIf(nameof(OfflineMode), true)] private string PlatformIP = "127.0.0.1";
        [SerializeField, HideIf(nameof(OfflineMode), true)] private ushort PlatformPort = 4298;

        [SerializeField, HideIf(nameof(OfflineMode), true)] private string CustomerID = "dev";
        [SerializeField, HideIf(nameof(OfflineMode), true)] private string CustomerKey = "dev";
        [SerializeField] private string InstanceSuffix = "dev";
        [SerializeField] private ServerConnectionSettings WorldSubStoreFTPSettings = new("dev", "dev", "127.0.0.1", 21);
        [SerializeField, EndGroup] private ServerConnectionSettings InstancingServerSettings  = new("dev", "dev", "127.0.0.1", 4297);
        #endregion


        #region Provider Interfaces
        private PlatformService _platformService;
        IPlatformService IPlatformProvider.PlatformService { 
            get 
            {
                if (_platformService == null)
                    OnEnable();

                return _platformService as IPlatformService;
            }
        }
        public string GameObjectName => gameObject.name;
        #endregion


        private void OnEnable() //TODO - handle reconnect
        {
            PlatformAPI.PlatformProvider = this;

            if (!Application.isPlaying || _platformService != null)
                return;

            Debug.Log("init platform");

            string instanceCode = $"{SceneManager.GetActiveScene().name}-{InstanceSuffix}";

            PlatformPersistentDataHandler platformPersistentDataHandler = FindFirstObjectByType<PlatformPersistentDataHandler>();
            if (platformPersistentDataHandler == null)
            {
                platformPersistentDataHandler = new GameObject("PlatformSettingsHandler").AddComponent<PlatformPersistentDataHandler>();
                platformPersistentDataHandler.SetDefaults(CustomerID, CustomerKey, instanceCode, WorldSubStoreFTPSettings, InstancingServerSettings);
            }
            
            _platformService = PlatformServiceFactory.Create(platformPersistentDataHandler);

            //False if we're in the hub/intro scene for the first time. 
            bool customerSettingsFound = true;
            if (customerSettingsFound)
                _platformService.ConnectToPlatform(IPAddress.Parse(PlatformIP), PlatformPort, instanceCode);
            // else, wait for the /intro scene to tell us to, after we've logged in.
        }

        private void FixedUpdate()
        {
            _platformService?.MainThreadUpdate();
        }

        private void OnDisable()
        {
            if (!Application.isPlaying)
                return;

            _platformService?.TearDown();
        }
    }

    // public class DebugPlatformService : IPlatformService
    // {
    //     public bool IsConnectedToServer => true;

    //     //TODO, when the user changes their settings, save to player prefs, also, LOAD from player prefs!
    //     public InstanceNetworkSettings InstanceNetworkSettings { get; }
    //     public UserSettingsPersistable UserSettings { get; }

    //     public FTPNetworkSettings FTPNetworkSettings { get; }

    //     //private IPlayerSettingsProvider _playerSettingsProvider;

    //     private readonly bool _exchangeDebugUserSettingsWithPlayerPrefs;

    //     public event Action OnConnectedToServer;

    //     public DebugPlatformService(InstanceNetworkSettings instanceNetworkSettingsDebug, FTPNetworkSettings ftpNetworkSetingsDebug, UserSettingsPersistable userSettingsDebug, bool exchangeDebugUserSettingsWithPlayerPrefs)
    //     {
    //         Debug.LogWarning($"No platform service provider found, using debug platform service. ");
    //         InstanceNetworkSettings = instanceNetworkSettingsDebug;
    //         FTPNetworkSettings = ftpNetworkSetingsDebug;
    //         UserSettings = userSettingsDebug;
    //         _exchangeDebugUserSettingsWithPlayerPrefs = exchangeDebugUserSettingsWithPlayerPrefs;
    //     }

    //     public void RequestInstanceAllocation(string worldName, string instanceSuffix)
    //     {
    //         Debug.Log($"Request instance allocation to {worldName}-{instanceSuffix} from debug platform service");
    //     }

    //     public void TearDown()
    //     {
    //         Debug.Log("Tear down debug platform service");
    //     }

    //     // public void SetupForNewInstance(IPlayerSettingsProvider playerSettingsProvider)
    //     // {
    //     //     if (_playerSettingsProvider != null)
    //     //         _playerSettingsProvider.OnLocalChangeToPlayerSettings -= HandleUserSettingsChanged;

    //     //     _playerSettingsProvider = playerSettingsProvider;
    //     //     _playerSettingsProvider.OnLocalChangeToPlayerSettings += HandleUserSettingsChanged;
    //     // }

    //     public void HandleUserSettingsChanged()
    //     {
    //         Debug.Log("Debug PlatformService detected change to user settings");
    //         if (_exchangeDebugUserSettingsWithPlayerPrefs)
    //         {
    //             //TODO, save to player prefs
    //         }
    //     }
    // }
}

/*
    PlayerService creates the 
*/
