using System;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Codice.Client.BaseCommands;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.SceneManagement;
using ViRSE.Core.Player;
using ViRSE.Core.Shared;
using ViRSE.Networking;
using ViRSE.Core;
using ViRSE.Core.VComponents;
using static InstanceSyncSerializables;
using static ViRSE.Core.Shared.CoreCommonSerializables;

namespace ViRSE.InstanceNetworking
{
    [ExecuteInEditMode]
    public class V_InstanceIntegration : MonoBehaviour, IMultiplayerSupport //, IInstanceNetworkSettingsReceiver
    {
        #region Inspector Fields
        // [DynamicHelp(nameof(_settingsMessage))]
        [SerializeField] private bool _connectOnStart = true;

        // private string _settingsMessage => _instanceNetworkSettingsProviderPresent ?
        //     $"Debug network settings can be found on the {_instanceNetworkSettingsProvider.GameObjectName} gameobject" :
        //     "If not connecting automatically, details should be passed via the API";
        #endregion

        [SerializeField, Disable, HideLabel, IgnoreParent] private ConnectionStateDebugWrapper _connectionStateDebug;

        [SerializeField, HideInInspector] private LocalClientIdWrapper LocalClientIDWrapper = new();
        [Serializable] public class LocalClientIdWrapper { public ushort LocalClientID = ushort.MaxValue; }

        private InstanceService _instanceService;
        private WorldStateSyncer _worldStateSyncer;
        private PlayerSyncer _playerSyncer;

        #region Core-Facing Interfaces
        //TODO - Could we follow the pattern set out by the VCs? Can we just stick this wiring in an interface?
        public void RegisterStateModule(IStateModule stateModule, string stateType, string goName)
        {
            if (_worldStateSyncer == null)
                OnEnable();

            _worldStateSyncer.RegisterStateModule(stateModule, stateType, goName);
        }
        
        public void RegisterLocalPlayer(ILocalPlayerRig localPlayerRig)
        {
            if (_playerSyncer == null)
                OnEnable();

            _playerSyncer.RegisterLocalPlayer(localPlayerRig);
        }
        
        public void DeregisterLocalPlayer() 
        {
            //If there is no player syncer, no need to deregister - TODO, Not sure if the player should even be in charge of registering and derigstering 
            //TODO, the player is trying to follow the same pattern as syncables, but it doesn't seem needed 
            //Syncables could also just emit a "Destroy" event that the syncer listens to 
            //TODO, maybe there's some way to use the service locator to store the syncables?
            //Syncables could register themselves with the service locator, then they wouldn't need to call register/derigster themselves 
            //More wiring, but looser coupling
            if (_playerSyncer != null) 
                _playerSyncer.DeregisterLocalPlayer();
        }

        public bool IsEnabled => enabled && gameObject.activeInHierarchy;
        public string GameObjectName => gameObject.name;
        #endregion

        #region Debug Interfaces 
        //TODO, think about exactly we want to serve to the customer... OnRemotePlayerConnectd? GetRemotePlayers?
        public bool IsConnectedToServer => _connectionStateDebug.ConnectionState == ConnectionState.Connected;
        public ushort LocalClientID => LocalClientIDWrapper.LocalClientID;
        public InstancedInstanceInfo InstanceInfo => _instanceService.InstanceInfo; //TODO, don't want to expose this
        public event Action<InstancedInstanceInfo> OnInstanceInfoChanged;
        public event Action OnDisconnectedFromServer;
        #endregion

        private void OnEnable()
        {
            if (!Application.isPlaying)
            {
                ViRSECoreServiceLocator.Instance.MultiplayerSupport = this;  
                return;
            }

            //But if we expose this to the customer... how does it go into the service locator???
            //Ok, we cam just make some base class for the settings provider that we can expose to the plugin, the plugin-defined settings provider can just inherit from that. The base can worry about putting itself into the service locator 
            if (ViRSENonCoreServiceLocator.Instance.InstanceNetworkSettingsProvider == null)
            {
                Debug.LogError("Can't boot instance integration, no network settings provider found");
                return;
            }

            if (_instanceService == null)
            {
                _instanceService = InstanceServiceFactory.Create(LocalClientIDWrapper, _connectOnStart, _connectionStateDebug);
                _instanceService.OnConnectedToServer += HandleConnectToServer; //TODO, maybe these events can go into the connection debug wrapper thing?
                _instanceService.OnInstanceInfoChanged += HandleReceiveInstanceInfo;
            }

            if (_worldStateSyncer == null)
                _worldStateSyncer = new WorldStateSyncer(_instanceService);

            if (_playerSyncer == null)
                _playerSyncer = PlayerSyncerFactory.Create(_instanceService);
        }

        private void FixedUpdate()
        {
            _instanceService.NetworkUpdate(); 

            //TODO - maybe the service should emit an update event that the others listen to?
            _worldStateSyncer.NetworkUpdate();
            _playerSyncer.NetworkUpdate();
        }

        private void HandleConnectToServer() 
        {
            _instanceService.OnConnectedToServer -= HandleConnectToServer;
            _instanceService.OnDisconnectedFromServer += HandleDisconnectFromServer; //TODO, maybe these events can go into the connection debug wrapper thing?

        }

        private void HandleReceiveInstanceInfo(InstancedInstanceInfo instanceInfo) 
        {
            OnInstanceInfoChanged?.Invoke(instanceInfo);
        }

        private void HandleDisconnectFromServer()
        {
            _instanceService.OnDisconnectedFromServer -= HandleDisconnectFromServer;
            OnDisconnectedFromServer?.Invoke();
        }

        private void OnDisable()
        {
            if (!Application.isPlaying)
                return;

            Debug.Log($"<color=red>Instance Integration disabled</color>");
            _connectionStateDebug.ConnectionState = ConnectionState.LostConnection;
            _instanceService.DisconnectFromServer();
            _playerSyncer.TearDown();
            _playerSyncer = null;
        }

        private void OnDestroy() 
        {
            if (!Application.isPlaying)
                return;

            _instanceService.TearDown();
            _instanceService = null;

            _connectionStateDebug.ConnectionState = ConnectionState.NotYetConnected;
        }
    }

    [Serializable]
    public class ConnectionStateDebugWrapper
    {
        [SerializeField, Disable, IgnoreParent] public ConnectionState ConnectionState = ConnectionState.NotYetConnected;
    }

    public enum ConnectionState { NotYetConnected, FetchingConnectionSettings, Connecting, Connected, LostConnection }

}
