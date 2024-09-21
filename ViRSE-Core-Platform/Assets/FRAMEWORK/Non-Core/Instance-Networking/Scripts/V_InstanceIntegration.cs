using System.Linq;
using System.Net;
using UnityEditorInternal;
using UnityEngine;
using ViRSE.Core.Player;
using ViRSE.Core.Shared;
using ViRSE.PluginRuntime;
using ViRSE.PluginRuntime.VComponents;
using static ViRSE.Core.Shared.CoreCommonSerializables;

namespace ViRSE.InstanceNetworking
{
    //TODO - Enforce GO name
    [ExecuteInEditMode]
    public class V_InstanceIntegration : MonoBehaviour, IMultiplayerSupport //, IInstanceNetworkSettingsReceiver
    {
        #region Inspector Fields
        [DynamicHelp(nameof(_settingsMessage))]
        [SerializeField, HideIf(nameof(_instanceNetworkSettingsProviderPresent), true)] private bool _connectAutomatically = true;
        [SerializeField, HideIf(nameof(_usingNetworkSettingsFromInspector), true)] private InstanceNetworkSettings _networkSettings;
        private bool _usingNetworkSettingsFromInspector => _instanceNetworkSettingsProviderPresent || !_connectAutomatically;
        private string _settingsMessage => _instanceNetworkSettingsProviderPresent ?
            "Debug network settings can be found on " + _instanceNetworkSettingsProvider.GameObjectName :
            "If not connecting automatically, details should be passed via the API";
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

            if (_instanceService != null)
                return;

            //Debug.Log("SCENE SYNCER AWAKE! enabled? " + enabled);

            GameObject playerSpawner = GameObject.Find("PlayerSpawner");
            PlayerPresentationConfig playerPresentationConfig = playerSpawner ? playerSpawner.GetComponent<V_PlayerSpawner>().PresentationConfig : null;

            //We pass these dependencies now, but they may change before the actual connection happens 
            //TODO - maybe that means it makes more sense to pass these on connect, rather than on create?
            _instanceService = PluginSyncServiceFactory.Create(_networkSettings, playerPresentationConfig);

            if (_instanceNetworkSettingsProviderPresent)
            {
                if (_instanceNetworkSettingsProvider.AreInstanceNetworkingSettingsReady)
                    HandleSettingsProviderReady();
                else
                    _instanceNetworkSettingsProvider.OnInstanceNetworkSettingsReady += HandleSettingsProviderReady;
            }
            else if (_connectAutomatically)
            {
                ConnectToServer();
            }
        }

        private void HandleSettingsProviderReady()
        {
            _instanceNetworkSettingsProvider.OnInstanceNetworkSettingsReady -= HandleSettingsProviderReady;

            //Bit of a bodge, we want to preserve the actual object, because the SyncService is using it
            _networkSettings.IP = _instanceNetworkSettingsProvider.InstanceNetworkSettings.IP;
            _networkSettings.Port = _instanceNetworkSettingsProvider.InstanceNetworkSettings.Port;
            _networkSettings.InstanceCode = _instanceNetworkSettingsProvider.InstanceNetworkSettings.InstanceCode;

            Debug.Log("<color=green>Network settings set " + _networkSettings.IP + "</color>"); 
            ConnectToServer();
        }


        public void ConnectToServer() //TODO - expose to plugin
        {
            _instanceService.ConnectToServer(); //TODO, should be passing network settings here instead 
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
        }


        private void OnDisable()
        {
            if (!Application.isPlaying)
            {
                //ViRSECoreServiceLocator.Instance.MultiplayerSupport = null;
                return;
            }

            _instanceService.TearDown();
        }
    }
}
