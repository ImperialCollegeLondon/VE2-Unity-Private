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

        private PrimaryServerService _primaryServerService;
        public IPrimaryServerService PrimaryServerService => _primaryServerService;
        public ILocalPlayerRig LocalPlayerRig => _localPlayerService;
        #endregion

        private LocalPlayerService _localPlayerService;
        private ServerType _serverType;

        public void Initialize(ServerType serverType)
        {
            _serverType = serverType;
            //DontDestroyOnLoad(gameObject);

            if (_serverType == ServerType.Offline)
            {
                HandleUserSettingsReady(null); //TODO - read in PlayerSettings from PlayerPrefs
            }
            else
            {
                Debug.Log("Make server");
                GameObject primaryServerServiceGO = GameObject.Instantiate(primaryServerServicePrefab);
                _primaryServerService = primaryServerServiceGO.GetComponent<PrimaryServerService>();

                _primaryServerService.OnPlayerSettingsReady += HandleUserSettingsReady;
                _primaryServerService.Initialize(serverType);
            }
        }

        //If we're not on the platform, what are we doing with player settings?
        private void HandleUserSettingsReady(UserSettings userSettings) //This should maybe be from the platform?
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
