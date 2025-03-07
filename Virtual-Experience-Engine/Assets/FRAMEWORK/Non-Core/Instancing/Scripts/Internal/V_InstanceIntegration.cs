using System;
using UnityEngine;
using UnityEngine.Events;
using VE2.Core.Player.API;
using VE2.Core.VComponents.API;
using VE2.NonCore.Instancing.API;
using static VE2.NonCore.Platform.API.PlatformPublicSerializables;

namespace VE2.NonCore.Instancing.Internal
{
    [Serializable]
    internal class InstanceCommsHandlerConfig
    {
        [Min(0), Tooltip("Artifical delay added to *sending* all instance networking messages from this player, in ms.")]
        [SerializeField] private float _artificialAddedPing;
        public float ArtificialAddedPing { get => _artificialAddedPing; private set => _artificialAddedPing = value >= 0 ? value : 0; }
    }

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

        [Space(10)]

        [SerializeField, HideLabel, IgnoreParent] private InstanceCommsHandlerConfig _config = new();

        [Space(10)]

        [Help("These settings will be used when testing in editor. In build, the platform service will provide the correct settings.")]
        [SerializeField, BeginGroup("Debug Settings"), DisableInPlayMode] private ServerConnectionSettings _debugServerSettings = new("dev", "dev", "127.0.0.1", 4297);
        [SerializeField, EndGroup, DisableInPlayMode] private string _debugInstanceCode = "Misc-Dev-00-NoVersion";
        #endregion


        #region Runtime data
        [SerializeField, Disable, HideLabel, IgnoreParent] private ConnectionStateDebugWrapper _connectionStateDebug;
        [SerializeField, HideInInspector] private LocalClientIdWrapper _localClientIDWrapper = new();
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

            _instanceService = InstanceServiceFactory.Create(_localClientIDWrapper, _connectOnStart, _connectionStateDebug, _debugServerSettings, _debugInstanceCode, _config);
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
