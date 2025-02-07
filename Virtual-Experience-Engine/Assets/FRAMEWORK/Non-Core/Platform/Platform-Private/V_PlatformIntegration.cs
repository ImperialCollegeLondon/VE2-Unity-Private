using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using VE2.Common;
using VE2.NonCore.Platform.Private;
using static VE2.Common.CommonSerializables;
using static VE2.PlatformNetworking.PlatformSerializables;

namespace VE2.PlatformNetworking
{
    [ExecuteInEditMode]
    public class V_PlatformIntegration : MonoBehaviour //TODO: Some interface for instance allocation, IInstanceNetworkSettingsProvider, IPlayerSettingsProvider, IFTPNetworkSettingsProvider
    {

        [Title("Debug Connection Settings")]
        [Help("If starting in this scene rather than the hub (e.g, when testing in the editor), these settings will be used.")]
        [BeginGroup("Fallback settings"), SerializeField] private bool OfflineMode = false;
        [SerializeField, HideIf(nameof(OfflineMode), true)] private string PlatformIP = "127.0.0.1";
        [SerializeField, HideIf(nameof(OfflineMode), true)] private ushort PlatformPort = 4298;
        [SerializeField, HideIf(nameof(OfflineMode), true)] private string CustomerID;
        [SerializeField, HideIf(nameof(OfflineMode), true)] private string CustomerKey;
        [SerializeField] private string FallbackPlatformInstanceSuffix = "dev";

        private PlatformService _platformService;

        public IPlatformService PlatformService {
            get {
                if (_platformService == null)
                    OnEnable();

                return _platformService;
            }
        }

        #region Shared Interfaces 
        public string GameObjectName => gameObject.name;
        public bool IsEnabled => enabled && gameObject.activeInHierarchy;

        public NetworkSettings NetworkSettings => throw new NotImplementedException();
        #endregion


        private void OnEnable() //TODO - handle reconnect
        {
            // if (platformProviderGO != null)
            //     _platformService = platformProviderGO.GetComponent<IPlatformServiceProvider>().PlatformService;
            // else
            //     _platformService = new DebugPlatformService(DebugInstanceNetworkSettings, DebugFTPNetworkSettings, _debugUserSettings, _exchangeDebugUserSettingsWithPlayerPrefs);

            string ipAddress;
            ushort portNumber;
            string customerID;
            string customerKey;
            string instanceCode;

            //Get customerLogin settings 
            //Get instance settings
            //InstanceService will also need those two, PLUS instance IP address settings
            PlayerPresentationConfig playerPresentationConfig = VE2CoreServiceLocator.Instance.PlayerSettingsHandler.PlayerPresentationConfig;

           


            _platformService = PlatformServiceFactory.Create();

            //_platformService.SetupForNewInstance(this);

            if (_platformService.IsConnectedToServer)
                HandlePlatformServiceReady();
            else
                _platformService.OnConnectedToServer += HandlePlatformServiceReady;
        }


        private void HandlePlatformServiceReady()
        {
            //Allows us to fake the UI in the inspector
            //We need to be sure to set the inspector's version of UserSettings to point towards the PlatformService's version
            //Otherwise, changing the settings in the inspector wont change the settings in the platform
           // _debugUserSettings = _platformService.UserSettings;
        }

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

            //if (_platformService != null)
            //    _platformService.OnConnectedToServer -= HandlePlatformServiceReady;
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
