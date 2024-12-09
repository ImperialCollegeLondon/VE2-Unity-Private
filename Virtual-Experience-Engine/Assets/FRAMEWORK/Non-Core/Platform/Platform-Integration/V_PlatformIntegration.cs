using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using VE2.Common;
using static VE2.Common.CommonSerializables;

namespace VE2.PlatformNetworking
{
    [ExecuteInEditMode]
    public class V_PlatformIntegration : MonoBehaviour, IInstanceNetworkSettingsProvider, IPlayerSettingsProvider, IFTPNetworkSettingsProvider
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
        [SerializeField, IgnoreParent] private FTPNetworkSettings DebugFTPNetworkSettings = new("127.0.0.1", 21, "testUN", "testPW");

        private IPlatformService _platformService;

        public IPlatformService PlatformService {
            get {
                if (_platformService == null)
                    OnEnable();

                return _platformService;
            }
        }

        #region Common network interfaces 
        public bool AreNetworkingSettingsReady => PlatformService.IsConnectedToServer;
        public event Action OnNetworkSettingsReady { add { PlatformService.OnConnectedToServer += value; } remove { PlatformService.OnConnectedToServer -= value; } }
        #endregion

        #region Instance-Networking Interfaces
        public InstanceNetworkSettings InstanceNetworkSettings => PlatformService.InstanceNetworkSettings;
        #endregion

        #region FTP-Networking Interfaces
        public NetworkSettings FTPNetworkSettings => PlatformService.FTPNetworkSettings;
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

        public NetworkSettings NetworkSettings => throw new NotImplementedException();
        #endregion


        private void OnEnable() //TODO - handle reconnect
        {
            if (!Application.isPlaying)
            {
                VE2NonCoreServiceLocator.Instance.InstanceNetworkSettingsProvider = this;
                VE2CoreServiceLocator.Instance.PlayerSettingsProvider = this;
                return;
            }

            GameObject platformProviderGO = GameObject.Find("PlatformServiceProvider");

            if (platformProviderGO != null)
                _platformService = platformProviderGO.GetComponent<IPlatformServiceProvider>().PlatformService;
            else
                _platformService = new DebugPlatformService(DebugInstanceNetworkSettings, DebugFTPNetworkSettings, _debugUserSettings, _exchangeDebugUserSettingsWithPlayerPrefs);

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

        public FTPNetworkSettings FTPNetworkSettings { get; }

        private IPlayerSettingsProvider _playerSettingsProvider;

        private readonly bool _exchangeDebugUserSettingsWithPlayerPrefs;

        public event Action OnConnectedToServer;

        public DebugPlatformService(InstanceNetworkSettings instanceNetworkSettingsDebug, FTPNetworkSettings ftpNetworkSetingsDebug, UserSettingsPersistable userSettingsDebug, bool exchangeDebugUserSettingsWithPlayerPrefs)
        {
            Debug.LogWarning($"No platform service provider found, using debug platform service. ");
            InstanceNetworkSettings = instanceNetworkSettingsDebug;
            FTPNetworkSettings = ftpNetworkSetingsDebug;
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
