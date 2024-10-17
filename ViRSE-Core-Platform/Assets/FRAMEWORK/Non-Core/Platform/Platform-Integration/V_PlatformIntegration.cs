using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using ViRSE.Core;
using static ViRSE.Core.Shared.CoreCommonSerializables;

namespace ViRSE.PlatformNetworking
{
    [ExecuteInEditMode]
    public class V_PlatformIntegration : MonoBehaviour, IInstanceNetworkSettingsProvider, IPlayerSettingsProvider
    {
        #region Utlity
        // public bool PlayerSettingsProviderPresent => PlayerSettingsProvider != null;
        // public IPlayerSettingsProvider PlayerSettingsProvider => ViRSECoreServiceLocator.Instance.PlayerSettingsProvider;
        // [SerializeField, HideInInspector] public bool SettingsProviderPresent => PlayerSettingsProvider != null;
        #endregion

        [Title("Debug User Settings")]
        [Help("For mocking the user settings that will be sent to V_PlayerSpawner in the editor. When you export your built world to the platform, the platform will override these settings.")]
        [BeginGroup(Style = GroupStyle.Round), SerializeField] private bool _exchangeDebugUserSettingsWithPlayerPrefs = true;

        [SpaceArea(spaceAfter: 10, Order = 0)]
        [EditorButton(nameof(NotifyProviderOfChangeToUserSettings), "Update user settings", activityType: ButtonActivityType.OnPlayMode)]
        [EndGroup(Order = 50), SerializeField, IgnoreParent, DisableIf(nameof(_shouldShowUserSettings), false)] private UserSettingsPersistable _debugUserSettings = new();
        private bool _shouldShowUserSettings => Application.isPlaying || !_exchangeDebugUserSettingsWithPlayerPrefs;

        //TODO, only show these if there actually is a player, and if these things are actually in the scene
        [Title("Debug Connection Settings")]
        [Help("For mocking the network settings that will be sent to V_InstanceIntegration in the editor. When you export your built world to the platform, the platform will override these settings.")]
        [SerializeField, IgnoreParent] private InstanceNetworkSettings DebugInstanceNetworkSettings = new("127.0.0.1", 4297, "dev-0");

        private IPlatformService _platformService;

        public IPlatformService PlatformService {
            get {
                if (_platformService == null)
                    OnEnable();

                return _platformService;
            }
        }

        #region Instance-Networking Interfaces
        public bool AreInstanceNetworkingSettingsReady => PlatformService.IsConnectedToServer;
        public event Action OnInstanceNetworkSettingsReady { add { PlatformService.OnConnectedToServer += value; } remove { PlatformService.OnConnectedToServer -= value; } }
        public InstanceNetworkSettings InstanceNetworkSettings => PlatformService.InstanceNetworkSettings;
        #endregion

        #region Player Settings Interfaces
        public bool ArePlayerSettingsReady => PlatformService.IsConnectedToServer;
        public event Action OnPlayerSettingsReady { add { PlatformService.OnConnectedToServer += value; } remove { PlatformService.OnConnectedToServer -= value; } }
        public UserSettingsPersistable UserSettings => PlatformService.UserSettings;
        public void NotifyProviderOfChangeToUserSettings() => OnLocalChangeToPlayerSettings?.Invoke(); 
        public event Action OnLocalChangeToPlayerSettings; //TODO, probably remove this once the UI is in place
        #endregion

        #region Shared Interfaces 
        public string GameObjectName => gameObject.name;
        public bool IsEnabled => enabled && gameObject.activeInHierarchy;
        #endregion


        private void OnEnable() //TODO - handle reconnect
        {
            if (!Application.isPlaying)
            {
                ViRSENonCoreServiceLocator.Instance.InstanceNetworkSettingsProvider = this;
                ViRSECoreServiceLocator.Instance.PlayerSettingsProvider = this;
                return;
            }

            GameObject platformProviderGO = GameObject.Find("PlatformServiceProvider");

            if (platformProviderGO != null)
                _platformService = platformProviderGO.GetComponent<IPlatformServiceProvider>().PlatformService;
            else
                _platformService = new DebugPlatformService(SceneManager.GetActiveScene().name + "-debug", DebugInstanceNetworkSettings, _debugUserSettings, _exchangeDebugUserSettingsWithPlayerPrefs);

            _platformService.SetupForNewInstance(this);

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
            _debugUserSettings = _platformService.UserSettings;
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

    public class DebugPlatformService : IPlatformService
    {
        public bool IsConnectedToServer => true;

        //TODO, when the user changes their settings, save to player prefs, also, LOAD from player prefs!
        public InstanceNetworkSettings InstanceNetworkSettings { get; }
        public UserSettingsPersistable UserSettings { get; }
        private IPlayerSettingsProvider _playerSettingsProvider;

        private readonly bool _exchangeDebugUserSettingsWithPlayerPrefs;

        public event Action OnConnectedToServer;

        public DebugPlatformService(string instanceCodeDebug, InstanceNetworkSettings networkSettingsDebug, UserSettingsPersistable userSettingsDebug, bool exchangeDebugUserSettingsWithPlayerPrefs)
        {
            Debug.LogWarning($"No platform service provider found, using debug platform service. ");
            InstanceNetworkSettings = new InstanceNetworkSettings("127.0.0.1", 4297, instanceCodeDebug);
            InstanceNetworkSettings = networkSettingsDebug;
            UserSettings = userSettingsDebug;
            _exchangeDebugUserSettingsWithPlayerPrefs = exchangeDebugUserSettingsWithPlayerPrefs;
        }

        public void RequestInstanceAllocation(string worldName, string instanceSuffix)
        {
            Debug.Log($"Request instance allocation to {worldName}-{instanceSuffix} from debug platform service");
        }

        public void TearDown()
        {
            Debug.Log("Tear down debug platform service");
        }

        public void SetupForNewInstance(IPlayerSettingsProvider playerSettingsProvider)
        {
            if (_playerSettingsProvider != null)
                _playerSettingsProvider.OnLocalChangeToPlayerSettings -= HandleUserSettingsChanged;

            _playerSettingsProvider = playerSettingsProvider;
            _playerSettingsProvider.OnLocalChangeToPlayerSettings += HandleUserSettingsChanged;
        }

        public void HandleUserSettingsChanged()
        {
            Debug.Log("Debug PlatformService detected change to user settings");
            if (_exchangeDebugUserSettingsWithPlayerPrefs)
            {
                //TODO, save to player prefs
            }
        }
    }
}
