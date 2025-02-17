using System;
using UnityEngine;
using VE2.Common;
using VE2.Core.VComponents.InteractableInterfaces;
using static InstanceSyncSerializables;

namespace VE2.InstanceNetworking
{
    [ExecuteInEditMode]
    public class V_InstanceIntegration : MonoBehaviour, IInstanceProvider, IWorldStateSyncProvider, ILocalClientIDProviderProvider
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
        [Serializable] public class LocalClientIdWrapper 
        { 
            private ushort _localClientID = ushort.MaxValue; 
            public ushort LocalClientID 
            {
                get => _localClientID;
                set 
                {
                    _localClientID = value;
                    OnLocalClientIDSet?.Invoke(value);
                }
            } 
            
            public event Action<ushort> OnLocalClientIDSet;
        }

        //We do this wiring here rather than the interface as the interface file needs to live in the VE2.common package
        #region Interfaces
        public bool IsEnabled => enabled && gameObject.activeInHierarchy;
        public string GameObjectName => gameObject.name;
        // public bool IsConnectedToServer => _connectionStateDebug.ConnectionState == ConnectionState.Connected;
        // public event Action OnConnectedToInstance { add => _instanceService.OnConnectedToInstance += value; remove => _instanceService.OnConnectedToInstance -= value; }
        // public event Action OnDisconnectedFromInstance { add => _instanceService.OnDisconnectedFromInstance += value; remove => _instanceService.OnDisconnectedFromInstance -= value; }
        // public bool IsHost => _instanceService._instanceInfoContainer.IsHost;
        // public ushort LocalClientID => _localClientIDWrapper.LocalClientID;
        #endregion


        public event Action OnConnectedToServer;

        private InstanceService _instanceService;
        public IInstanceService InstanceService {
            get 
            {
                if (_instanceService == null)
                    OnEnable();

                return _instanceService as IInstanceService;
            }
        }

        public IWorldStateSyncService WorldStateSyncService => _instanceService.WorldStateSyncService;
        public ILocalClientIDProvider LocalClientIDProvider => _instanceService;

        private void OnEnable()
        {
            InstancingAPI.InstanceProvider = this;
            PlayerLocator.LocalClientIDProviderProvider = this;
            VComponents_Locator.WorldStateSyncProvider = this;

            if (!Application.isPlaying || _instanceService != null)
            {
                PlayerLocator.LocalClientIDProviderProvider = this;  
                return;
            }

            //But if we expose this to the customer... how does it go into the service locator???
            //Ok, we cam just make some base class for the settings provider that we can expose to the plugin, the plugin-defined settings provider can just inherit from that. The base can worry about putting itself into the service locator 
            if (PlatformServiceLocator.PlatformService == null)
            {
                Debug.LogError("Can't boot instance integration, no platform service found");
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

        #region PlayerSyncer interfaces

        public void RegisterPlayerStateModule(IBaseStateModule module)
        {
            throw new NotImplementedException();
        }

        public void DeregisterPlayerStateModule(IBaseStateModule module)
        {
            throw new NotImplementedException();
        }

        public void RegisterInteractor(IInteractor interactor)
        {
            throw new NotImplementedException();
        }

        public void DeregisterInteractor(IInteractor interactor)
        {
            throw new NotImplementedException();
        }

        #endregion
    }

    [Serializable]
    public class ConnectionStateDebugWrapper
    {
        [SerializeField, Disable, IgnoreParent] public ConnectionState ConnectionState = ConnectionState.NotYetConnected;
    }

    public enum ConnectionState { NotYetConnected, WaitingForConnectionSettings, Connecting, Connected, LostConnection }

}
