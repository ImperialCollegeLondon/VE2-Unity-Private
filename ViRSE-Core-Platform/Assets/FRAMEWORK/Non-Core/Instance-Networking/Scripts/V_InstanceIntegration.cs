using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.SceneManagement;
using ViRSE.Core.Player;
using ViRSE.Core.Shared;
using ViRSE.PluginRuntime;
using ViRSE.PluginRuntime.VComponents;
using static ViRSE.Core.Shared.CoreCommonSerializables;

namespace ViRSE.InstanceNetworking
{
    [ExecuteInEditMode]
    public class V_InstanceIntegration : MonoBehaviour, IMultiplayerSupport //, IInstanceNetworkSettingsReceiver
    {
        #region Inspector Fields
        [DynamicHelp(nameof(_settingsMessage))]
        [SerializeField, HideIf(nameof(_instanceNetworkSettingsProviderPresent), true)] private bool _connectOnStart = true;
        [SerializeField, HideIf(nameof(_usingNetworkSettingsFromInspector), true)] private InstanceNetworkSettings _networkSettings;
        private bool _usingNetworkSettingsFromInspector => _instanceNetworkSettingsProviderPresent || !_connectOnStart;
        private string _settingsMessage => _instanceNetworkSettingsProviderPresent ?
            $"Debug network settings can be found on the {_instanceNetworkSettingsProvider.GameObjectName} gameobject" :
            "If not connecting automatically, details should be passed via the API";
        #endregion

        #region ConnectionDebug

        [SerializeField, Disable] private ConnectionState _connectionState = ConnectionState.Disconnected;
        private enum ConnectionState { Disconnected, FetchingConnectionSettings, Connecting, Connected }
        #endregion

        private PluginSyncService _instanceService;
        public PluginSyncService InstanceService {
            get {
                if (_instanceService == null)
                    OnEnable();

                return _instanceService;
            }
            set {
                _instanceService = (PluginSyncService)value;
            }
        }

        private bool _instanceNetworkSettingsProviderPresent => _instanceNetworkSettingsProvider != null;
        private IInstanceNetworkSettingsProvider _instanceNetworkSettingsProvider => ViRSENonCoreServiceLocator.Instance.InstanceNetworkSettingsProvider;


        #region Core-Facing Interfaces 
        public void RegisterStateModule(IStateModule stateModule, string stateType, string goName)
        {
            InstanceService.RegisterStateModule(stateModule, stateType, goName);
        }

        public void RegisterLocalPlayer(ILocalPlayerRig localPlayerRig)
        {
            InstanceService.RegisterLocalPlayer(localPlayerRig);
        }

        public bool IsEnabled => enabled && gameObject.activeInHierarchy;
        public string MultiplayerSupportGameObjectName => gameObject.name;
        #endregion


        //#region Platform-Facing Interfaces
        //public void SetInstanceNetworkSettings(InstanceNetworkSettings instanceNetworkSettings)         //TODO - need a customer facing version of this too
        //{
        //    _connectionDetails = instanceNetworkSettings;

        //    if (_instanceService != null)
        //    {
        //        ConnectToServer();
        //    }
        //}
        // #endregion

        private void OnEnable()
        {
            if (!Application.isPlaying)
            {
                ViRSECoreServiceLocator.Instance.MultiplayerSupport = this;  
                return;
            }

            if (_instanceService == null)
            {
                GameObject playerSpawner = GameObject.Find("PlayerSpawner"); //TODO - use service locator
                //Debug.Log("<color=cyan>SPAWNER null? " + (playerSpawner == null).ToString() + " SCENE = " + SceneManager.GetActiveScene().name + "</color>");
                //Debug.Log("<color=cyan>Config null??? " + (playerSpawner.GetComponent<V_PlayerSpawner>().SpawnConfig.playerSettings.PresentationConfig == null).ToString() + "</color>");
                PlayerConfig playerConfig = playerSpawner ? playerSpawner.GetComponent<V_PlayerSpawner>().SpawnConfig : null;
                //We pass these dependencies now, but they may change before the actual connection happens 
                //TODO - maybe that means it makes more sense to pass these on connect, rather than on create?
                _instanceService = PluginSyncServiceFactory.Create(playerConfig);
                _instanceService.OnConnectedToServer += () => _connectionState = ConnectionState.Connected;
                _instanceService.OnDisconnectedFromServer += () => _connectionState = ConnectionState.Disconnected;
            }

            if (_connectionState == ConnectionState.Disconnected)
            {
                if (_instanceNetworkSettingsProviderPresent)
                {
                    ConnectToServerOnceDetailsReady();
                }
                else if (_connectOnStart)
                {
                    ConnectToServer();
                }
            }
        }

        private void ConnectToServerOnceDetailsReady() 
        {
            _connectionState = ConnectionState.FetchingConnectionSettings;

            //On boot, we want to just connect if _connectOnStart is true
            if (_instanceNetworkSettingsProvider.AreInstanceNetworkingSettingsReady)
                HandleSettingsProviderReady();
            else
                _instanceNetworkSettingsProvider.OnInstanceNetworkSettingsReady += HandleSettingsProviderReady;
        }

        private void HandleSettingsProviderReady()
        {
            _instanceNetworkSettingsProvider.OnInstanceNetworkSettingsReady -= HandleSettingsProviderReady;

            _networkSettings = _instanceNetworkSettingsProvider.InstanceNetworkSettings;
            //Bit of a bodge, we want to preserve the actual object, because the SyncService is using it
            // _networkSettings.IP = _instanceNetworkSettingsProvider.InstanceNetworkSettings.IP;
            // _networkSettings.Port = _instanceNetworkSettingsProvider.InstanceNetworkSettings.Port;
            // _networkSettings.InstanceCode = _instanceNetworkSettingsProvider.InstanceNetworkSettings.InstanceCode;

            Debug.Log("<color=green>Network settings set " + _networkSettings.IP + "</color>");
            ConnectToServer();
        }

        private void ConnectToServer() 
        {
            _connectionState = ConnectionState.Connecting;
            _instanceService.ConnectToServer(_networkSettings); 
        }

        private void FixedUpdate()
        {
            _instanceService.NetworkUpdate(); //TODO, think about this... perhaps the syncers should be in charge of calling themselves?

            //Player will instantiate a player sync module 
            //That sync module will look for the network manager, calling the "register player" API will cause the Mono to create a new service, and give that service the player
            //The sync module will then need to create a new player syncer. 
            //So, does it make more sense for the newly created player syncer to handle its update, or should this be the service?
            //Kinda feel like that's the syncer's job? The service should just just be a comms layer, basically 
            //In that case though, it's jank to have the service be the thing that creates the syncers, no?

            //Maybe the SceneSyncer should actually create the WorldStateSyncer and PlayerSyncer, and pass them both a reference to the PluginSyncService... that way, the service doesn't have to actually do anything!
            //Feels like that's probably the cleanest pattern to use here...

            //Ok, so how should programmatic connection work?
            //Disabled if "connect automatically" is up, 
        }


        private void OnDisable()
        {
            if (!Application.isPlaying)
            {
                //ViRSECoreServiceLocator.Instance.MultiplayerSupport = null;
                return;
            }

            _instanceService.DisconnectFromServer();
            //_instanceService.TearDown();
            // _instanceService = null;
        }

        private void OnDestroy() 
        {
            if (!Application.isPlaying)
            {
                return;
            }

            _instanceService.TearDown();
            _instanceService.OnConnectedToServer -= () => _connectionState = ConnectionState.Connected;
            _instanceService.OnDisconnectedFromServer -= () => _connectionState = ConnectionState.Disconnected;
            _instanceService = null;

            _connectionState = ConnectionState.Disconnected;
        }
    }
}



