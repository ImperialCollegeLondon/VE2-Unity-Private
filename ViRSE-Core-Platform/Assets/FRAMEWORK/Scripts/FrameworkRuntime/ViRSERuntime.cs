using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using ViRSE.FrameworkRuntime.LocalPlayerRig;

namespace ViRSE.FrameworkRuntime
{
    public interface IFrameworkRuntime
    {
        public IPrimaryServerService PrimaryServerService { get; }
        public ILocalPlayerRig LocalPlayerRig { get; }

        public bool IsFrameworkReady { get; }
        public event Action OnFrameworkReady;

        public void Initialize(ServerType serverType);
    }

    public class ViRSERuntime : MonoBehaviour, IFrameworkRuntime
    {
        [SerializeField] private GameObject primaryServerServicePrefab;
        [SerializeField] private GameObject localPlayerRigPrefab;

        #region Plugin Runtime interfaces
        public bool IsFrameworkReady { get; private set; }
        public event Action OnFrameworkReady;

        public IPrimaryServerService PrimaryServerService { get; private set; }
        public ILocalPlayerRig LocalPlayerRig => _localPlayerService;
        #endregion

        private LocalPlayerService _localPlayerService;
        private ServerType _serverType;

        public void Initialize(ServerType serverType)
        {
            _serverType = serverType;
            DontDestroyOnLoad(gameObject);

            if (_serverType == ServerType.Offline)
            {
                HandleUserSettingsReady(null); //TODO - read in PlayerSettings from PlayerPrefs
            }
            else
            {
                Debug.Log("Make server");
                PrimaryServerService primaryServerService = Instantiate(primaryServerServicePrefab).GetComponent<PrimaryServerService>();
                DontDestroyOnLoad(primaryServerService);

                primaryServerService.OnPlayerSettingsReady += HandleUserSettingsReady;
                primaryServerService.Initialize(serverType);
            }
        }

        private void HandleUserSettingsReady(UserSettings userSettings)
        {
            if (userSettings == null)
                userSettings = UserSettings.GenerateDefaults();

            Debug.Log("Spawn player");
            GameObject localPlayerRigGO = Instantiate(localPlayerRigPrefab, transform);
            _localPlayerService = localPlayerRigGO.GetComponent<LocalPlayerService>();

            _localPlayerService.Initialize(userSettings);

            IsFrameworkReady = true;
            OnFrameworkReady?.Invoke();
        }
    }
}
