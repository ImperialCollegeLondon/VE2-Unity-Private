using System;
using System.Collections;
using UnityEngine;
using VE2.Common;
using VE2.Core.Common;
using static VE2.Common.CommonSerializables;

namespace VE2.Core.Player
{
    [Serializable]
    public class PlayerStateConfig : BaseStateConfig
    {
        //Events for state change (2d/vr)
    }

    [ExecuteAlways]
    public class V_PlayerSpawner : MonoBehaviour//, IPlayerSpawner //Should this be called "PlayerIntegration"?
    {
        //TODO, configs for each player, OnTeleport, DragHeight, FreeFlyMode, etc
        [SerializeField] public bool enableVR = false;
        [SerializeField] public bool enable2D = true;
        [SerializeField, IgnoreParent] public PlayerStateConfig playerStateConfig = new();

        private bool _transformDataSetup = false;
        private PlayerTransformData _playerTransformData = new();

        private IXRManagerWrapper _xrManagerWrapper;

        private PlayerService _playerService;
        private bool _xrInitialized = false;

        private void OnEnable() 
        {
            if (PlayerLocator.Instance.PlayerSettingsHandler == null)
                PlayerLocator.Instance.PlayerSettingsHandler = new GameObject("PlayerSettings").AddComponent<PlayerSettingsHandler>();

            if (!Application.isPlaying)
                return;

            _xrManagerWrapper = new XRManagerWrapper();

            if (!_transformDataSetup)
            {
                _playerTransformData.RootPosition = transform.position;
                _playerTransformData.RootRotation = transform.rotation;
                _playerTransformData.VerticalOffset = 1.7f;
                _transformDataSetup = true;
            }

            if (PlayerLocator.Instance.PlayerSettingsHandler == null) 
            {
                Debug.LogError("Error, V_PlayerSpawner cannot spawn player, no player settings provider found.");
                return;
            }

            if (enableVR)
                StartCoroutine(InitializeXR());

            if (enableVR && !_xrInitialized)
                StartCoroutine(InitializePlayerServiceAfterXRInit());
            else
                InitializePlayerService();
        }

        private IEnumerator InitializeXR()
        {
            yield return _xrManagerWrapper.InitializeLoader();

            if (_xrManagerWrapper.ActiveLoader == null)
            {
                Debug.LogError("Failed to initialize XR Loader.");
            }
            else
            {
                Debug.Log("XR initialized and subsystems started.");
                _xrInitialized = true;
            }
        }

        private IEnumerator InitializePlayerServiceAfterXRInit()
        {
            while (!_xrInitialized)
                yield return null;

            InitializePlayerService();
        }

        private void InitializePlayerService()
        {
            if (Application.platform == RuntimePlatform.Android && !Application.isEditor)
            {
                enableVR = true;
                enable2D = false;
            }

            _playerService = VE2PlayerServiceFactory.Create(_playerTransformData, playerStateConfig, enableVR, enable2D);
        }

        private void FixedUpdate() 
        {
            if (!Application.isPlaying)
                return;

            _playerService?.HandleFixedUpdate();
        }

        private void Update() 
        {
            if (!Application.isPlaying)
                return;

            _playerService?.HandleUpdate();
        }   

        private void OnDisable() 
        {
            if (!Application.isPlaying)
                return;
                
            Debug.Log("Disabling player spawner, service null? " + (_playerService == null));
            _playerService?.TearDown();
            _playerService = null;
        }
    }
}
