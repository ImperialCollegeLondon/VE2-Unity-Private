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
    [ExecuteInEditMode]
    public class V_SceneSyncer : MonoBehaviour, INetworkManager
    {
        [SerializeField] private bool _connectAutomatically = true;
        [SerializeField] private InstanceNetworkSettings _connectionDetails;

        private IInstanceNetworkSettingsProvider _instanceNetworkSettingsProvider;

        private PluginSyncService _pluginSyncService;
        private PluginSyncService PluginSyncService 
        {
            get {
                if (_pluginSyncService == null)
                    Awake();

                return _pluginSyncService;
            }
            set {
                _pluginSyncService = value;
            }
        }


        #region Core-Facing Interfaces 
        public void RegisterStateModule(IStateModule stateModule, string stateType, string goName)
        {
            PluginSyncService.RegisterStateModule(stateModule, stateType, goName);
        }
        public bool IsEnabled => enabled;
        #endregion

        //TODO, functions for letting the customer change the connection details 
        public void SetIPAddress()
        {
            //If on platform, don't allow
            //TODO, same deal for port number, and instance code 

            //TODO, public facing function for connect, disconnect.
        }

        private void Awake()
        {
            if (_pluginSyncService != null)
                return;

            if (!Application.isPlaying)
                return;

            Debug.Log("SCENE SYNCER AWAKE! enabled? " + enabled);

            GameObject playerSpawner = GameObject.Find("PlayerSpawner");
            PlayerPresentationConfig playerPresentationConfig = playerSpawner ? playerSpawner.GetComponent<V_PlayerSpawner>().PresentationConfig : null;

            //We pass these dependencies now, but they may change before the actual connection happens 
            //TODO - maybe that means it makes more sense to pass these on connect, rather than on create?
            _pluginSyncService = PluginSyncServiceFactory.Create(_connectionDetails, playerPresentationConfig);

            SearchForAndAssignSettingsProvider();

            if (_instanceNetworkSettingsProvider != null)
            {
                if (_instanceNetworkSettingsProvider.AreInstanceNetworkingSettingsReady)
                    HandleSettingsReady();
                else
                    _instanceNetworkSettingsProvider.OnInstanceNetworkSettingsReady += HandleSettingsReady;
            }
            else if (_connectAutomatically)
                ConnectToServer();
        }

        private void HandleSettingsReady()
        {
            _instanceNetworkSettingsProvider.OnInstanceNetworkSettingsReady -= HandleSettingsReady;

            _connectionDetails = _instanceNetworkSettingsProvider.InstanceNetworkSettings;

            if (_connectionDetails == null)
            {
                Debug.LogError("Error getting connection details from platform");
                return;
            }

            if (_connectAutomatically)
                ConnectToServer();
        }

        public void SearchForAndAssignSettingsProvider() //TODO, mmm, maybe we just assign this in the inspector??
        {
            MonoBehaviour[] monos = FindObjectsOfType<MonoBehaviour>();
            IInstanceNetworkSettingsProvider instanceNetworkSettingsProvider = monos.OfType<IInstanceNetworkSettingsProvider>().FirstOrDefault();

            if (instanceNetworkSettingsProvider != null && ((MonoBehaviour)instanceNetworkSettingsProvider).isActiveAndEnabled)
                _instanceNetworkSettingsProvider = instanceNetworkSettingsProvider;
        }

        public void ConnectToServer() //TODO - expose to plugin
        {
            PluginSyncService.ConnectToServer();
        }

        private void FixedUpdate()
        {
            PluginSyncService.NetworkUpdate(); //TODO, think about this... perhaps the syncers should be in charge of calling themselves?
            
            //Player will instantiate a player sync module 
            //That sync module will look for the network manager, calling the "register player" API will cause the Mono to create a new service, and give that service the player
            //The sync module will then need to create a new player syncer. 
            //So, does it make more sense for the newly created player syncer to handle its update, or should this be the service?
            //Kinda feel like that's the syncer's job? The service should just just be a comms layer, basically 
            //In that case though, it's jank to have the service be the thing that creates the syncers, no?

            //Maybe the SceneSyncer should actually create the WorldStateSyncer and PlayerSyncer, and pass them both a reference to the PluginSyncService... that way, the service doesn't have to actually do anything!
            //Feels like that's probably the cleanest pattern to use here...
        }


        private void OnEnable()
        {
            if (!Application.isPlaying)
            {
                BaseStateHolder[] baseStateConfigs = GameObject.FindObjectsOfType<BaseStateHolder>();

                foreach (BaseStateHolder baseStateConfig in baseStateConfigs)
                    baseStateConfig.BaseStateConfig.NetworkManager = this;

                return;
            }
        }

        private void OnDisable()
        {
            if (!Application.isPlaying)
            {
                BaseStateHolder[] baseStateConfigs = GameObject.FindObjectsOfType<BaseStateHolder>();

                foreach (BaseStateHolder baseStateConfig in baseStateConfigs)
                    baseStateConfig.BaseStateConfig.NetworkManager = null;
            }
        }
    }
}
