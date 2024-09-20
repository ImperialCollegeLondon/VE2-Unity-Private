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
    public class V_SceneSyncer : MonoBehaviour, INetworkManager, IInstanceNetworkSettingsReceiver
    {
        [SerializeField] private bool _connectAutomatically = true;

        [Help("If you have a V_PlatformIntegration in your scene, these settings will come from that component instead")]
        [SerializeField] private InstanceNetworkSettings _connectionDetails;

        private PluginSyncService _pluginSyncService;
        public INetworkManager PluginSyncService {
            get {
                if (_pluginSyncService == null)
                    OnEnable();

                return _pluginSyncService;
            }
            set {
                _pluginSyncService = (PluginSyncService)value;
            }
        }


        #region Core-Facing Interfaces 
        public void RegisterStateModule(IStateModule stateModule, string stateType, string goName)
        {
            PluginSyncService.RegisterStateModule(stateModule, stateType, goName);
        }
        public bool IsEnabled => enabled;
        #endregion


        #region Platform-Facing Interfaces
        public void SetInstanceNetworkSettings(InstanceNetworkSettings instanceNetworkSettings)         //TODO - need a customer facing version of this too
        {
            _connectionDetails = instanceNetworkSettings;

            if (_pluginSyncService != null)
            {
                ConnectToServer();
            }
            #endregion
        }

        private void OnEnable()
        {
            if (!Application.isPlaying)
            {
                BaseStateHolder[] baseStateConfigs = GameObject.FindObjectsOfType<BaseStateHolder>(); //TODO, this shouldn't be able to see the state holder

                foreach (BaseStateHolder baseStateConfig in baseStateConfigs)
                    baseStateConfig.BaseStateConfig.NetworkManager = this; //Not even really sure this should be the responsibility of the instance networking stuff 

                return;
            }

            if (_pluginSyncService != null)
                return;

            //Debug.Log("SCENE SYNCER AWAKE! enabled? " + enabled);

            GameObject playerSpawner = GameObject.Find("PlayerSpawner");
            PlayerPresentationConfig playerPresentationConfig = playerSpawner ? playerSpawner.GetComponent<V_PlayerSpawner>().PresentationConfig : null;

            //We pass these dependencies now, but they may change before the actual connection happens 
            //TODO - maybe that means it makes more sense to pass these on connect, rather than on create?
            _pluginSyncService = PluginSyncServiceFactory.Create(_connectionDetails, playerPresentationConfig);

            GameObject platformIntegrationGO = GameObject.Find("V_PlatformIntegration");    

            if (platformIntegrationGO == null && _connectAutomatically)
                ConnectToServer();
        }


        public void ConnectToServer() //TODO - expose to plugin
        {
            _pluginSyncService.ConnectToServer();
        }

        private void FixedUpdate()
        {
            _pluginSyncService.NetworkUpdate(); //TODO, think about this... perhaps the syncers should be in charge of calling themselves?

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
                BaseStateHolder[] baseStateConfigs = GameObject.FindObjectsOfType<BaseStateHolder>();

                foreach (BaseStateHolder baseStateConfig in baseStateConfigs)
                    baseStateConfig.BaseStateConfig.NetworkManager = null;
            }
            else
            {
                _pluginSyncService.TearDown();
            }
        }
    }
}
