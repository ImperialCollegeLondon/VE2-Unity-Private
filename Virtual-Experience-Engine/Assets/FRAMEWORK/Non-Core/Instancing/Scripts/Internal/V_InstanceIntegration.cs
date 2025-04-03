using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Events;
using VE2.Core.Player.API;
using VE2.Core.VComponents.API;
using VE2.NonCore.Instancing.API;
using VE2.NonCore.Platform.API;
using static VE2.NonCore.Platform.API.PlatformPublicSerializables;
using System.Collections.Generic;

namespace VE2.NonCore.Instancing.Internal
{
    [Serializable]
    internal class InstanceCommsHandlerConfig
    {
        [Min(0), Tooltip("Artifical delay added to *sending* all instance networking messages from this player, in ms.")]
        [SerializeField] private float _artificialAddedPingMs;
        public float ArtificialAddedPing { get => _artificialAddedPingMs; private set => _artificialAddedPingMs = value >= 0 ? value : 0; }
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
        [SerializeField, DisableInPlayMode] private bool _connectOnStart = true;
        [SerializeField, Disable, HideLabel, IgnoreParent] private ConnectionStateWrapper _connectionStateDebug;

        [SerializeField, IgnoreParent, DisableInPlayMode] private DebugInstancingSettings _debugServerSettings = new();

        [Serializable]
        private class DebugInstancingSettings
        {
            [Help("These settings will be used when testing in editor. In build, the platform service will provide the correct settings.")]
            [SerializeField, BeginGroup("Debug Settings"), DisableInPlayMode] internal string IpAddress = "127.0.0.1";
            [SerializeField, DisableInPlayMode] internal ushort Port = 4297;
            [SerializeField, EndGroup, DisableInPlayMode] internal string InstanceCode = "00";
        }
        [Space(10)]
        [SerializeField, HideLabel, IgnoreParent] private InstanceCommsHandlerConfig _config = new();
        [Space(10)]
        #endregion


        #region Runtime data
        [SerializeField, HideInInspector] private LocalClientIdWrapper _localClientIDWrapper = new();
        private SyncInfosContainer _syncInfosContainer = new(); //We store this here and inject it in so it persists between lifecycles of the instance service
        #endregion

        //We do this wiring here rather than the interface as the interface file needs to live in the VE2.common package
        #region provider Interfaces
        public bool IsEnabled => gameObject != null && enabled && gameObject.activeInHierarchy;
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
        public IWorldStateSyncService WorldStateSyncService => ((InstanceService)InstanceService)?.WorldStateSyncService;

        public ushort LocalClientID => _localClientIDWrapper.LocalClientID;
        public event Action<ushort> OnClientIDReady {add => _localClientIDWrapper.OnLocalClientIDSet += value; remove => _localClientIDWrapper.OnLocalClientIDSet -= value; }
        #endregion

        private bool _bootErrorLogged = false;
        private RectTransform _debugUIRect;

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

            if (PlatformAPI.PlatformService == null)
            {
                if (!_bootErrorLogged)
                {
                    _bootErrorLogged = true;
                    Debug.LogError("Can't boot instance integration, V_PlatformIntegration must be present in the scene, even when using debug settings");
                }
                return;
            }

            ServerConnectionSettings instancingSettings = ((IPlatformServiceInternal)PlatformAPI.PlatformService).GetInstanceServerSettingsForCurrentWorld();
            string instanceCode;

            if (instancingSettings != null)
            {
                instanceCode = PlatformAPI.PlatformService.CurrentInstanceCode;
            }
            else 
            {
                if (Application.isEditor)
                {
                    instancingSettings = new ServerConnectionSettings("username", "pass", _debugServerSettings.IpAddress, _debugServerSettings.Port);
                    instanceCode = $"NoCat-{SceneManager.GetActiveScene().name}-{_debugServerSettings.InstanceCode}-NoVersion";
                }
                else
                {
                    if (!_bootErrorLogged)
                    {
                        _bootErrorLogged = true;
                        Debug.LogError("Can't boot instance integration, no instancing settings found - cannot use debug instance settings in Build");
                    }
                    return;
                }   
            }

            _instanceService = InstanceServiceFactory.Create(_localClientIDWrapper, _connectOnStart, _connectionStateDebug, instancingSettings, instanceCode, _config, _syncInfosContainer);

            if (Application.isEditor)
            {
                GameObject debugUIHolder = GameObject.Instantiate(Resources.Load<GameObject>("HostDebugRectHolder"));
                _debugUIRect = debugUIHolder.transform.GetChild(0).GetComponent<RectTransform>();
                (PlayerAPI.Player as IPlayerServiceInternal).AddPanelTo2DOverlayUI(_debugUIRect);
                GameObject.Destroy(debugUIHolder);
            }
        }

        private void FixedUpdate()
        {
            _instanceService?.HandleUpdate();
        }

        private void OnDisable()
        {
            if (!Application.isPlaying)
                return;

            _instanceService?.TearDown();
            _instanceService = null;

            if (_debugUIRect != null)
            {
                DestroyImmediate(_debugUIRect.gameObject);
            }
        }

        private void OnDestroy()
        {
            _syncInfosContainer._syncInfosAgainstIDs.Clear();            
        }
    }

    [Serializable]
    public class ConnectionStateWrapper
    {
        [SerializeField, Disable, IgnoreParent] public ConnectionState ConnectionState = ConnectionState.NotYetConnected;
    }

    public enum ConnectionState { NotYetConnected, Connecting, Connected, LostConnection }

}
