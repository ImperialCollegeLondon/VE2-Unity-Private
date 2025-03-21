using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using VE2.NonCore.Platform.API;

namespace VE2.NonCore.Platform.Internal
{
    [ExecuteInEditMode]
    internal class V_PlatformIntegration : MonoBehaviour, IPlatformProvider 
    {
        // #region Debug settings
        // [Help("If starting in this scene rather than the Hub (e.g, when testing in the editor), these settings will be used.")]
        // [BeginGroup("Debug settings"), SerializeField] private bool OfflineMode = false;
        // [SerializeField, HideIf(nameof(OfflineMode), true)] private string PlatformIP = "127.0.0.1";
        // [SerializeField, HideIf(nameof(OfflineMode), true)] private ushort PlatformPort = 4298;

        // // [SerializeField, HideIf(nameof(OfflineMode), true)] private string CustomerID = "dev";
        // // [SerializeField, HideIf(nameof(OfflineMode), true)] private string CustomerKey = "dev";
        // // [SerializeField] private string InstanceSuffix = "dev";
        // [SerializeField] private ServerConnectionSettings WorldSubStoreFTPSettings = new("dev", "dev", "127.0.0.1", 21);
        // [SerializeField, EndGroup] private ServerConnectionSettings InstancingServerSettings  = new("dev", "dev", "127.0.0.1", 4297);
        // #endregion


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

            PlatformPersistentDataHandler platformPersistentDataHandler = FindFirstObjectByType<PlatformPersistentDataHandler>();
            if (platformPersistentDataHandler == null) 
                platformPersistentDataHandler = new GameObject("PlatformSettingsHandler").AddComponent<PlatformPersistentDataHandler>();
            
            _platformService = PlatformServiceFactory.Create(platformPersistentDataHandler);

            if (SceneManager.GetActiveScene().name != "Hub" && platformPersistentDataHandler.PlatformServerConnectionSettings != null) //If we're in a plugin, and have come from hub
                _platformService.ConnectToPlatform();
            //If we're in hub, or started in plugin. Don't connect. The hub will give us connection settings and fire off the connection.... unless those connection settings instead come from an "Intro" scene.
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
