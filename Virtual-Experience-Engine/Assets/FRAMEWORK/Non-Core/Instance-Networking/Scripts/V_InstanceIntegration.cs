using System;
using UnityEngine;
using VE2.Common;
using static InstanceSyncSerializables;

namespace VE2.InstanceNetworking
{
    [ExecuteInEditMode]
    public class V_InstanceIntegration : MonoBehaviour, IMultiplayerSupport 
    {
        #region Inspector Fields
        // [DynamicHelp(nameof(_settingsMessage))]
        [EditorButton(nameof(ConnectToServerOnceDetailsReady), "Connect", activityType: ButtonActivityType.OnPlayMode)] //TODO only works if _connectOnStart is false, look into other EditorButton packages
        [SerializeField] private bool _connectOnStart = true;

        // private string _settingsMessage => _instanceNetworkSettingsProviderPresent ?
        //     $"Debug network settings can be found on the {_instanceNetworkSettingsProvider.GameObjectName} gameobject" :
        //     "If not connecting automatically, details should be passed via the API";
        #endregion

        [SerializeField, Disable, HideLabel, IgnoreParent] private ConnectionStateDebugWrapper _connectionStateDebug;

        [SerializeField, HideInInspector] private LocalClientIdWrapper _localClientIDWrapper = new();
        [Serializable] public class LocalClientIdWrapper { public ushort LocalClientID = ushort.MaxValue; }

        //We do this wiring here rather than the interface as the interface file needs to live in the VE2.common package
        #region Interfaces
        public bool IsEnabled => enabled && gameObject.activeInHierarchy;
        public string GameObjectName => gameObject.name;
        public bool IsConnectedToServer => _connectionStateDebug.ConnectionState == ConnectionState.Connected;
        public event Action OnConnectedToInstance { add => _instanceService.OnConnectedToInstance += value; remove => _instanceService.OnConnectedToInstance -= value; }
        public event Action OnDisconnectedFromInstance { add => _instanceService.OnDisconnectedFromInstance += value; remove => _instanceService.OnDisconnectedFromInstance -= value; }
        public event Action<ushort> OnHostChanged { add => _instanceService.OnHostChanged += value; remove => _instanceService.OnHostChanged -= value; }
        public ushort LocalClientID => _localClientIDWrapper.LocalClientID;
        public bool IsHost => _instanceService._instanceInfoContainer.IsHost;
        #endregion

        #region Temp debug Interfaces //TODO: remove once UI is in 
        public InstancedInstanceInfo InstanceInfo => _instanceService._instanceInfoContainer.InstanceInfo; //TODO, don't want to expose this
        public event Action<InstancedInstanceInfo> OnInstanceInfoChanged { 
            add => _instanceService._instanceInfoContainer.OnInstanceInfoChanged += value; 
            remove => _instanceService._instanceInfoContainer.OnInstanceInfoChanged -= value; 
        }
        public event Action<int> OnPingUpdate
        {
            add => _instanceService.OnPingUpdate += value;
            remove => _instanceService.OnPingUpdate -= value;
        }
        #endregion

        /*
            Maybe we should split the interface up a bit... 
            One for things that need to be internal facing (or at least, facing core) 
            And one for things that need to be exposed to the plugin
            The things that wire into the plugin, can have a standard IV_InstanceIntegration interface 
        */

        private InstanceService _instanceService;

        private void OnEnable()
        {
            if (!Application.isPlaying)
            {
                VE2CoreServiceLocator.Instance.MultiplayerSupport = this;  
                return;
            }

            //But if we expose this to the customer... how does it go into the service locator???
            //Ok, we cam just make some base class for the settings provider that we can expose to the plugin, the plugin-defined settings provider can just inherit from that. The base can worry about putting itself into the service locator 
            if (VE2NonCoreServiceLocator.Instance.InstanceNetworkSettingsProvider == null)
            {
                Debug.LogError("Can't boot instance integration, no network settings provider found");
                return;
            }

            _instanceService = InstanceServiceFactory.Create(_localClientIDWrapper, _connectOnStart, _connectionStateDebug);
        }

        public void ConnectToServerOnceDetailsReady() 
        {
            if (!_connectOnStart)
                _instanceService?.ConnectToServerWhenReady();
        } 

        private void FixedUpdate()
        {
            _instanceService.NetworkUpdate();
        }

        private void OnDisable()
        {
            if (!Application.isPlaying)
                return;

            _instanceService.TearDown();
            _instanceService = null;
        }
    }

    [Serializable]
    public class ConnectionStateDebugWrapper
    {
        [SerializeField, Disable, IgnoreParent] public ConnectionState ConnectionState = ConnectionState.NotYetConnected;
    }

    public enum ConnectionState { NotYetConnected, WaitingForConnectionSettings, Connecting, Connected, LostConnection }

}
