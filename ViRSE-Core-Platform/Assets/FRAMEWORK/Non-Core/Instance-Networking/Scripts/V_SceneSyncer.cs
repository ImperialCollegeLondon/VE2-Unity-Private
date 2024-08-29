using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;
using ViRSE;
using ViRSE.Core.Player;
using ViRSE.Core.Shared;
using ViRSE.FrameworkRuntime;
using ViRSE.PluginRuntime;
using ViRSE.PluginRuntime.VComponents;

namespace ViRSE.InstanceNetworking
{
    [ExecuteInEditMode]
    public class V_SceneSyncer : MonoBehaviour, INetworkManager
    {
        //TODO hide this if platform integration isn't present 
        [SerializeField] private bool _isPlatform = false; //If true, will use the platform's server type
        [SerializeField] private bool _connectAutomatically = true; //If false, connection must be programmatic 

        [SerializeField] private string _instanceCode = "dev"; //Can be changed by the customer code(?) 

        //If isPlatform, these settings will be overriden by whatever the platform says
        [SerializeField] private ServerType _serverType = ServerType.Local;
        [SerializeField] private string _localServerIP = "127.0.0.1";
        [SerializeField] private string _remoteServerIP = "";
        [SerializeField] private int portNumber = 4297;

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

        private void Awake()
        {
            if (_pluginSyncService != null)
                return;

            Debug.Log("SCENE SYNCER AWAKE a!");

            if (_serverType != ServerType.Offline)
            {
                PlayerPresentationConfig playerPresentationConfig = null;
                GameObject playerSpawner = GameObject.Find("PlayerSpawner");
                if (playerSpawner != null)
                {
                    playerPresentationConfig = playerSpawner.GetComponent<V_PlayerSpawner>().PresentationConfig;
                }

                _pluginSyncService = PluginSyncServiceFactory.Create(playerPresentationConfig);
            }

            if (_connectAutomatically && !_isPlatform)
            {
                ConnectToServer();
            }
        }

        //The platform will call this with overrides
        public void ConnectToServer(string ipAddressOverride = null, int portNumberOverride = -1, string instanceCodeOverride = null)
        {
            string ipAddressString;
            
            if (ipAddressOverride == null)
                ipAddressString = _serverType == ServerType.Local ? _localServerIP : _remoteServerIP;
            else
                ipAddressString = ipAddressOverride;

            if (portNumberOverride == -1)
                portNumberOverride = portNumber;

            if (instanceCodeOverride != null)
                _instanceCode = instanceCodeOverride;

            if (IPAddress.TryParse(ipAddressString, out IPAddress ipAddress) == false)
                throw new System.Exception("Invalid IP address, could not connect to server");

            PluginSyncService.ConnectToServer(ipAddress, portNumber, _instanceCode);
        }

        private void FixedUpdate()
        {
            PluginSyncService.NetworkUpdate();
        }

        public void RegisterStateModule(IStateModule stateModule, string stateType, string goName)
        {
            PluginSyncService.RegisterStateModule(stateModule, stateType, goName);
        }

        //TODO, API to change instance code, and to connect/disconnect
        //API for connect, disconnect, change instance code 

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


//#if UNITY_EDITOR
//            [UnityEditor.InitializeOnLoadMethod]
//        private static void RegisterDomainReloadCallback()
//        {
//            UnityEditor.AssemblyReloadEvents.afterAssemblyReload += HandleReload;
//            UnityEditor.AssemblyReloadEvents.beforeAssemblyReload += HandlePreReload;
//        }
//#endif
    }
}
