using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using VE2.Common;
using VE2.NonCore.Platform.Private;
using static VE2.Common.CommonSerializables;
using static VE2.Platform.API.PlatformPublicSerializables;
using static VE2.Platform.Internal.PlatformSerializables;

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
    public class V_PlatformIntegration : MonoBehaviour, IPlatformProvider //, IPlatformService, IPlatformServiceInternal
    {
        //[Title("Debug Connection Settings")]
        [Help("If starting in this scene rather than the Hub (e.g, when testing in the editor), these settings will be used.")]
        [BeginGroup("Debug settings"), SerializeField] private bool OfflineMode = false;
        [SerializeField, HideIf(nameof(OfflineMode), true)] private string PlatformIP = "127.0.0.1";
        [SerializeField, HideIf(nameof(OfflineMode), true)] private ushort PlatformPort = 4298;

        [SerializeField, HideIf(nameof(OfflineMode), true)] private string CustomerID = "dev";
        [SerializeField, HideIf(nameof(OfflineMode), true)] private string CustomerKey = "dev";
        [SerializeField] private string InstanceSuffix = "dev";
        [SerializeField] private ServerConnectionSettings WorldSubStoreFTPSettings = new("dev", "dev", "127.0.0.1", 21);
        [SerializeField, EndGroup] private ServerConnectionSettings InstancingServerSettings  = new("dev", "dev", "127.0.0.1", 4297);

        public string GameObjectName => gameObject.name;

        private PlatformService _platformService;
        IPlatformService IPlatformProvider.PlatformService { 
            get 
            {
                if (_platformService == null)
                    OnEnable();

                return _platformService as IPlatformService;
            }
        }


        private void OnEnable() //TODO - handle reconnect
        {
            PlatformServiceLocator.PlatformProvider = this;

            if (!Application.isPlaying || _platformService != null)
            {
                //Maybe find the settings handlers and show their inspectors, if possible
                return;
            }

            string instanceCode = $"{SceneManager.GetActiveScene().name}-{InstanceSuffix}";

            PlatformSettingsHandler platformSettingsHandler = FindFirstObjectByType<PlatformSettingsHandler>();
            if (platformSettingsHandler == null)
            {
                platformSettingsHandler = new GameObject("PlatformSettingsHandler").AddComponent<PlatformSettingsHandler>();
                platformSettingsHandler.SetDefaults(CustomerID, CustomerKey, instanceCode, WorldSubStoreFTPSettings, InstancingServerSettings);
            }
            
            _platformService = PlatformServiceFactory.Create(platformSettingsHandler);

            //TODO: - should these just be wired in through the constructor?
            //Why would we ever actually need to wait for these? or change them?
            //We'll pull them out of the settings handler
            //Apart from the very first time in the intro scene...
            //We'll need to wait for the intro to tell us to connect to the platform, and with what details
            //because that's the first time, we can just assume that, if the connection settings are missing, we're in the intro scene
            // string ipAddress = PlatformIP;
            // ushort portNumber = PlatformPort;
            // string customerID = CustomerID;
            // string customerKey = CustomerKey;

            //Get customerLogin settings 
            //Get instance settings
            //InstanceService will also need those two, PLUS instance IP address settings
            //PlayerPresentationConfig playerPresentationConfig = PlayerLocator.Player.PlayerPresentationConfig;

            Debug.Log("Create instance code: " + instanceCode);
            //False if we're in the hub for the first time. 
            bool customerSettingsFound = true;
            if (customerSettingsFound)
                _platformService.ConnectToPlatform(IPAddress.Parse(PlatformIP), PlatformPort, instanceCode);
            // else, wait for the hub to tell us to, after we've logged in.


            // if (_platformService.IsConnectedToServer)
            //     HandlePlatformServiceReady();
            // else
            //     _platformService.OnConnectedToServer += HandlePlatformServiceReady;
        }


        // private void HandlePlatformServiceReady()
        // {
        //     //Invoke events? 
        //     IsAuthFailed = false;
        //     IsConnectedToServer = true;
        //     OnConnectedToServer?.Invoke();
        // }

        // private void HandleAuthFailed()
        // {
        //     IsConnectedToServer = false;
        //     IsAuthFailed = true;
        //     OnAuthFailed?.Invoke();
        // }

        private void FixedUpdate()
        {
            _platformService?.MainThreadUpdate();
        }

        private void OnDisable()
        {
            if (!Application.isPlaying)
            {
                //ViRSENonCoreServiceLocator.Instance.InstanceNetworkSettingsProvider = null;
                return;
            }

            _platformService?.TearDown();

            //if (_platformService != null)
            //    _platformService.OnConnectedToServer -= HandlePlatformServiceReady;
        }

        // void IPlatformServiceInternal.RequestInstanceAllocation(string worldName, string instanceSuffix)
        // {
        //     throw new NotImplementedException();
        // }

        // ServerConnectionSettings IPlatformServiceInternal.GetInstanceServerSettingsForWorld(string worldName)
        // {
        //     throw new NotImplementedException();
        // }

        // ServerConnectionSettings IPlatformServiceInternal.GetInstanceServerSettingsForCurrentWorld()
        // {
        //     throw new NotImplementedException();
        // }
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
