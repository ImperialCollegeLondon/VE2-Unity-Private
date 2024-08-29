using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;
using ViRSE;
using ViRSE.Core.Player;
using ViRSE.Core.Shared;
using ViRSE.FrameworkRuntime;
using ViRSE.PluginRuntime;

namespace ViRSE.InstanceNetworking
{
    public class V_SceneSyncer : MonoBehaviour, INetworkManager
    {
        //TODO hide this if platform integration isn't present 
        [SerializeField] private bool _isPlatform = false; //If true, will use the platform's server type
        [SerializeField] private bool _connectAutomatically = true; //If false, connection must be programmatic 

        [SerializeField] private string _instanceCode; //Can be changed by the customer code(?) 

        //If isPlatform, these settings will be overriden by whatever the platform says
        [SerializeField] private ServerType _serverType = ServerType.Local;
        [SerializeField] private string _localServerIP = "127.0.0.1";
        [SerializeField] private string _remoteServerIP = "";
        [SerializeField] private int portNumber = 4296;

        //Should just take an IP address

        private PluginSyncService _pluginSyncService;

        void Awake()
        {
            if (_serverType != ServerType.Offline)
            {
                PlayerPresentationConfig playerPresentationConfig = null;
                GameObject playerSpawner = GameObject.Find("PlayerSpawner");
                if (playerSpawner != null)
                {
                    playerPresentationConfig = playerSpawner.GetComponent<V_PlayerSpawner>().PresentationConfig;
                }

                _pluginSyncService = PluginSyncServiceFactory.Create(playerPresentationConfig);

                if (_serverType == ServerType.Local)
                {
                    //TODO - start local server
                }

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

            _pluginSyncService.ConnectToServer(ipAddress, portNumber, _instanceCode);
        }

        private void FixedUpdate()
        {
            _pluginSyncService.NetworkUpdate();
        }

        //TODO, bit bodgey
        public void RegisterStateModule(IStateModule stateModule, string stateType, string goName)
        {
            _pluginSyncService.RegisterStateModule(stateModule, stateType, goName);
        }

        //TODO, API to change instance code, and to connect/disconnect
        //API for connect, disconnect, change instance code 
    }
}
