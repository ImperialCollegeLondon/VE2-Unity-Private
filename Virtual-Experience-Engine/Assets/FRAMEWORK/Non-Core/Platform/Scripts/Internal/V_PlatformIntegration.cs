using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using VE2.Common.API;
using VE2.NonCore.Platform.API;

namespace VE2.NonCore.Platform.Internal
{
    [Serializable]
    internal class PlatformServiceConfig
    {
        [Title("Platform Settings")]
        [SerializeField, BeginGroup] public UnityEvent OnBecomeAdmin;
        [SerializeField, EndGroup] public UnityEvent OnLoseAdmin;
    }

    [ExecuteInEditMode]
    internal class V_PlatformIntegration : MonoBehaviour, IPlatformProvider 
    {
        [SerializeField, IgnoreParent] private PlatformServiceConfig _config = new();

        // #region Debug settings
        // [Help("If starting in this scene rather than the Hub (e.g, when testing in the editor), these settings will be used.")]
        // [BeginGroup("Debug settings"), SerializeField] private bool OfflineMode = false;
        // [SerializeField, HideIf(nameof(OfflineMode), true)] private string PlatformIP = "127.0.0.1";
        // [SerializeField, HideIf(nameof(OfflineMode), true)] private ushort PlatformPort = 4298;

        // // [SerializeField, HideIf(nameof(OfflineMode), true)] private string CustomerID = "dev";
        // // [SerializeField, HideIf(nameof(OfflineMode), true)] private string CustomerKey = "dev";
        // // [SerializeField] private string InstanceSuffix = "dev";
        // [SerializeField] private ServerConnectionSettings WorldSubStoreFTPSettings = new("dev", "dev", "127.0.0.1", 21);
        // [SerializeField, EndGroup] private ServerConnectionSettings InstancingServerSettings  = new("dev", "dev", "127.0.0.1", 4297);
        // #endregion


        #region Provider Interfaces
        private IPlatformServiceInternal _platformService;
        IPlatformService IPlatformProvider.PlatformService { 
            get 
            {
                if (_platformService == null)
                    OnEnable();

                return _platformService as IPlatformService;
            }
        }
        public string GameObjectName => gameObject.name;
        #endregion


        private void OnEnable() //TODO - handle reconnect
        {
            VE2API.PlatformProvider = this;

            if (!Application.isPlaying || _platformService != null)
                return;

            PlatformPersistentDataHandler platformPersistentDataHandler = FindFirstObjectByType<PlatformPersistentDataHandler>();
            if (platformPersistentDataHandler == null)
                platformPersistentDataHandler = new GameObject("PlatformSettingsHandler").AddComponent<PlatformPersistentDataHandler>();

            bool inHub = SceneManager.GetActiveScene().name == "Hub";
            bool comeFromHub = platformPersistentDataHandler.PlatformServerConnectionSettings != null;

            if (inHub || comeFromHub)
                _platformService = PlatformServiceFactory.Create(_config, platformPersistentDataHandler);
            else
                _platformService = new DebugPlatformService(_config, VE2API.LocalAdminIndicator as ILocalAdminIndicatorWritable); //If we're in a plugin, and have come from hub, we don't have connection settings. So use the debug service.

            if (!inHub)
                _platformService.ConnectToPlatform();
            //If we're in hub, don't connect. The hub will give us connection settings and fire off the connection.... unless those connection settings instead come from an "Intro" scene.
        }

        private void FixedUpdate()
        {
            _platformService?.MainThreadUpdate();
        }

        private void OnDisable()
        {
            if (!Application.isPlaying)
                return;

            _platformService?.TearDown();
        }
    }

    internal class DebugPlatformService : IPlatformServiceInternal
    {
        public string PlayerDisplayName => "debugPlayerName";

        public ushort LocalClientID => 1;

        public bool IsAuthFailed => false;

        public List<(string, int)> ActiveWorldsNamesAndVersions => new();

        public bool IsConnectedToServer { get; private set; } = false;

        public string CurrentInstanceNumber => "debugInstanceNumber";

        public string CurrentWorldName => "DebugWorldName";

        PlatformPublicSerializables.InstanceCode IPlatformServiceInternal.CurrentInstanceCode => new PlatformPublicSerializables.InstanceCode(CurrentWorldName, CurrentInstanceNumber, 1);

        Dictionary<PlatformPublicSerializables.InstanceCode, PlatformPublicSerializables.PlatformInstanceInfo> IPlatformServiceInternal.InstanceInfos => new();

        public event Action OnAuthFailed;
        public event Action OnConnectedToServer;
        public event Action OnLeavingInstance;

        event Action<Dictionary<PlatformPublicSerializables.InstanceCode, PlatformPublicSerializables.PlatformInstanceInfo>> IPlatformServiceInternal.OnInstanceInfosChanged
        {
            add
            {
                //throw new NotImplementedException();
            }

            remove
            {
                //throw new NotImplementedException();
            }
        }

        public void ConnectToPlatform()
        {
            IsConnectedToServer = true;
            Debug.Log("Connected to debug platform service");

            try
            {
                OnConnectedToServer?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error when emitting OnConnectedToServer: {ex.Message}, {ex.StackTrace}");
            }
        }

        public void ReturnToHub()
        {
            Debug.Log("Return to hub clicked");
        }

        public void TearDown() { }

        PlatformPublicSerializables.ServerConnectionSettings IPlatformServiceInternal.GetInstanceServerSettingsForCurrentWorld() => null;

        PlatformPublicSerializables.ServerConnectionSettings IPlatformServiceInternal.GetInstanceServerSettingsForWorld(string worldName) => null;

        PlatformPublicSerializables.ServerConnectionSettings IPlatformServiceInternal.GetInternalWorldStoreFTPSettings() => null;

        PlatformPublicSerializables.ServerConnectionSettings IPlatformServiceInternal.GetWorldSubStoreFTPSettingsForCurrentWorld() => null;

        void IPlatformServiceInternal.RequestInstanceAllocation(PlatformPublicSerializables.InstanceCode instanceCode)
        {
            throw new NotImplementedException();
        }

        void IPlatformServiceInternal.UpdateSettings(PlatformPublicSerializables.ServerConnectionSettings serverConnectionSettings, PlatformPublicSerializables.InstanceCode instanceCode)
        {
            throw new NotImplementedException();
        }

        public void MainThreadUpdate() {}

        public void GrantLocalPlayerAdmin()
        {
            Debug.Log("Debug platform service: Granting local player admin");
            _localAdminIndicatorWritable.SetLocalAdminStatus(true);

            try
            {
                OnBecomeAdmin?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error when emitting OnBecomeAdmin: {ex.Message}, {ex.StackTrace}");
            }
        }
        public void RevokeLocalPlayerAdmin()
        {
            Debug.Log("Debug platform service: Revoking local player admin");
            _localAdminIndicatorWritable.SetLocalAdminStatus(false);

            try
            {
                OnLoseAdmin?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error when emitting OnLoseAdmin: {ex.Message}, {ex.StackTrace}");
            }
        }

        public bool IsLocalPlayerAdmin => _localAdminIndicatorWritable.IsLocalAdmin; //TODO: In real service, this comes from client info
        public UnityEvent OnBecomeAdmin => _config.OnBecomeAdmin;
        public UnityEvent OnLoseAdmin => _config.OnLoseAdmin;

        private readonly PlatformServiceConfig _config;
        private readonly ILocalAdminIndicatorWritable _localAdminIndicatorWritable;

        public DebugPlatformService(PlatformServiceConfig config, ILocalAdminIndicatorWritable localAdminIndicatorWritable)
        {
            _config = config;
            _localAdminIndicatorWritable = localAdminIndicatorWritable;
        }
    }
}

