using System;
using UnityEngine;
using VE2.Common;

namespace VE2.InstanceNetworking
{
    //Note, ILocalClientIDProvider is implemented here, NOT on the service - it needs to exsit at edit-time
    //Since the platform inits the player, and instancing inits the platform, we can't have the player init the instancing
    //Otherwise we'd have a stack overflow, instead, provide ID from the mono here, without initing the instancing service
    [ExecuteInEditMode]
    internal class V_InstanceIntegration : MonoBehaviour, IInstanceProvider, IWorldStateSyncProvider, ILocalClientIDProvider
    {
        #region Inspector frontend
        private void DebugConnect() => _instanceService.ConnectToInstance();
        private void DebugDisconnect() => _instanceService.DisconnectFromInstance();
        [EditorButton(nameof(DebugConnect), "Connect", activityType: ButtonActivityType.OnPlayMode)] 
        [EditorButton(nameof(DebugDisconnect), "Disconnect", activityType: ButtonActivityType.OnPlayMode)] 
        [SerializeField] private bool _connectOnStart = true;
        #endregion


        #region Runtime data
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
        #endregion

        //We do this wiring here rather than the interface as the interface file needs to live in the VE2.common package
        #region provider Interfaces
        public bool IsEnabled => enabled && gameObject.activeInHierarchy;
        public string GameObjectName => gameObject.name;
        private InstanceService _instanceService;
        public IInstanceService InstanceService {
            get 
            {
                if (_instanceService == null)
                    OnEnable();

                return _instanceService as IInstanceService;
            }
        }

        //Note - we want to go through the public getter for this, so we trigger lazy init
        public IWorldStateSyncService WorldStateSyncService => ((InstanceService)InstanceService).WorldStateSyncService;

        public ushort LocalClientID => _localClientIDWrapper.LocalClientID;
        public event Action<ushort> OnClientIDReady {add => _localClientIDWrapper.OnLocalClientIDSet += value; remove => _localClientIDWrapper.OnLocalClientIDSet -= value; }
        #endregion

        private void OnEnable()
        {
            InstancingAPI.InstanceProvider = this;
            PlayerAPI.LocalClientIDProvider = this;
            VComponentsAPI.WorldStateSyncProvider = this;

            if (!Application.isPlaying || _instanceService != null)
            {
                PlayerAPI.LocalClientIDProvider = this;  
                return;
            }

            Debug.Log("init instancing");

            if (PlatformAPI.PlatformService == null)
            {
                Debug.LogError("Can't boot instance integration, no platform service found");
                return;
            }

            _instanceService = InstanceServiceFactory.Create(_localClientIDWrapper, _connectOnStart, _connectionStateDebug);
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

    public enum ConnectionState { NotYetConnected, Connecting, Connected, LostConnection }

}
