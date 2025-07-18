using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Events;
using VE2.Core.Player.API;
using VE2.NonCore.Instancing.API;
using VE2.NonCore.Platform.API;
using static VE2.NonCore.Platform.API.PlatformPublicSerializables;
using VE2.Common.API;
using NUnit.Framework;

namespace VE2.NonCore.Instancing.Internal
{
    [Serializable]
    internal class InstancingConfig
    {
        [SerializeField, IgnoreParent] internal InstanceServiceConfig InstanceServiceConfig = new();
        [SerializeField, IgnoreParent] internal DebugInstancingSettings DebugInstancingSettings = new();
    }

    [Serializable]
    internal class InstanceServiceConfig
    {
        [Title("Instance Events", Order = -2)]
        [Help("Events for connecting to and disconnecting from the instance will emit the client ID of the local player", Order = 10)]
        [BeginGroup(Order = -1), SerializeField] internal UnityEvent<ushort> OnConnectedToInstance = new();
        [SerializeField, SpaceArea(spaceAfter: 10)] internal UnityEvent<ushort> OnDisconnectedFromInstance = new();

        [Help("Events for remote clients joining and leaving the instance will emit the client ID of the remote player in question", Order = 12)]
        [SerializeField] internal  UnityEvent<ushort> OnRemoteClientJoinedInstance = new();
        [SerializeField] internal  UnityEvent<ushort> OnRemoteClientLeftInstance = new();
        [Space(10)]
        [SerializeField] internal  UnityEvent OnBecomeHost = new();
        [EndGroup, SerializeField] internal  UnityEvent OnBecomeNonHost = new();
    }

    [Serializable]
    internal class DebugInstancingSettings
    {
        [Title("Debug Settings", Order = -2)]
        [Help("These settings will be used when testing in editor. In build, the platform service will provide the correct settings.", Order = 10)]
        [SerializeField, BeginGroup(Order = -1), DisableInPlayMode] internal string IpAddress = "127.0.0.1";
        [SerializeField, DisableInPlayMode] internal ushort Port = 4297;
        [SerializeField, DisableInPlayMode] internal string InstanceNumber = "00";

        [SerializeField, IgnoreParent, EndGroup] public ArtificialLatencySettings ArtificialLatencySettings = new();
    }

    [Serializable]
    public class ArtificialLatencySettings
    {
        [Tooltip("Artifical delay added to *sending* all instance networking messages from this player, in ms.")]
        [Min(0), SerializeField] public float ArtificialAddedPing = 0f;
    }

    [ExecuteAlways]
    [DisallowMultipleComponent]
    internal class V_InstanceIntegration : MonoBehaviour, IInstanceProvider
    {
        #region Inspector frontend
        private void DebugConnect() => _instanceService.ConnectToInstance();
        private void DebugDisconnect() => _instanceService.DisconnectFromInstance();
        [EditorButton(nameof(DebugConnect), "Connect", activityType: ButtonActivityType.OnPlayMode)]
        [EditorButton(nameof(DebugDisconnect), "Disconnect", activityType: ButtonActivityType.OnPlayMode)]
        [SerializeField, DisableInPlayMode] private bool _connectOnStart = true;
        [SerializeField, Disable, HideLabel, IgnoreParent] private ConnectionStateWrapper _connectionStateDebug;

        [SerializeField, IgnoreParent, DisableInPlayMode] private InstancingConfig _instancingConfig = new();
        #endregion

        //We do this wiring here rather than the interface as the interface file needs to live in the VE2.common package
        #region provider Interfaces
        public bool IsEnabled => this != null && gameObject != null && enabled && gameObject.activeInHierarchy;
        private InstanceService _instanceService;
        public IInstanceService InstanceService
        {
            get
            {
                if (_instanceService == null)
                    OnEnable();

                return _instanceService as IInstanceService;
            }
        }

        public ushort LocalClientID => VE2API.LocalClientIdWrapper.Value;
        public event Action<ushort> OnClientIDReady { add => VE2API.LocalClientIdWrapper.OnClientIDReady += value; remove => VE2API.LocalClientIdWrapper.OnClientIDReady -= value; }
        #endregion

        private bool _bootErrorLogged = false;
        private RectTransform _debugUIRect;

        private void OnEnable()
        {
            VE2API.InstancingServiceProvider = this;

            if (!Application.isPlaying)
                return;

            if (VE2API.PlatformService == null) //TODO - should point to VE2API
            {
                if (!_bootErrorLogged)
                {
                    _bootErrorLogged = true;
                    Debug.LogError("Can't boot instance integration, V_PlatformIntegration must be present in the scene, even when using debug settings");
                }
                return;
            }

            ServerConnectionSettings instancingSettings = ((IPlatformServiceInternal)VE2API.PlatformService).GetInstanceServerSettingsForCurrentWorld();
            InstanceCode instanceCode;
            ArtificialLatencySettings artificialLatencySettingsToUse;

            if (instancingSettings != null)
            {
                instanceCode = ((IPlatformServiceInternal)VE2API.PlatformService).CurrentInstanceCode;
                artificialLatencySettingsToUse = new() { ArtificialAddedPing = 0 };
            }
            else
            {
                if (Application.isEditor)
                {
                    instancingSettings = new ServerConnectionSettings("username", "pass", _instancingConfig.DebugInstancingSettings.IpAddress, _instancingConfig.DebugInstancingSettings.Port);
                    instanceCode = new InstanceCode(SceneManager.GetActiveScene().name, _instancingConfig.DebugInstancingSettings.InstanceNumber, 0);
                    artificialLatencySettingsToUse = _instancingConfig.DebugInstancingSettings.ArtificialLatencySettings;
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

            if (instancingSettings.ServerAddress == "127.0.0.1" && Application.isEditor)
                InstancingUtils.BootLocalServerIfNotAlreadyRunning();

            _instanceService = InstanceServiceFactory.Create(_connectOnStart, _connectionStateDebug, instancingSettings, _instancingConfig.InstanceServiceConfig, instanceCode, artificialLatencySettingsToUse);

            if (Application.isEditor)
            {
                GameObject debugUIHolder = GameObject.Instantiate(Resources.Load<GameObject>("HostDebugRectHolder"));
                _debugUIRect = debugUIHolder.transform.GetChild(0).GetComponent<RectTransform>();
                (VE2API.Player as IPlayerServiceInternal)?.AddPanelTo2DOverlayUI(_debugUIRect);
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
    }

    [Serializable]
    public class ConnectionStateWrapper
    {
        [SerializeField, Disable, IgnoreParent] public ConnectionState ConnectionState = ConnectionState.NotYetConnected;
    }

    public enum ConnectionState { NotYetConnected, Connecting, Connected, LostConnection }

}
