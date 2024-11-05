using System;
using System.Collections;
using UnityEngine;
using VE2.Common;
using VE2.Core.Common;

namespace VE2.Core.Player
{
    [Serializable]
    public class PlayerStateConfig : BaseStateConfig
    {
        //Events for state change (2d/vr)
        //Maybe also an event for appearance changed?
        //The state module should probably also have methods for moving the player

    }

    //TODO, consolidate all this into one config class?
    public class V_PlayerSpawner : MonoBehaviour//, IPlayerSpawner //Should this be called "PlayerIntegration"?
    {
        //TODO, configs for each player, OnTeleport, DragHeight, FreeFlyMode, etc
        [SerializeField] public bool enableVR;
        [SerializeField] public bool enable2D;
        [SerializeField, IgnoreParent] public PlayerStateConfig playerStateConfig = new();

        private bool _transformDataSetup = false;
        private PlayerTransformData _playerTransformData = new();

        private PlayerService _playerService;
        private bool _xrInitialized = false;

        private void OnEnable() 
        {
            if (!_transformDataSetup)
            {
                _playerTransformData.RootPosition = transform.position;
                _playerTransformData.RootRotation = transform.rotation;
                _transformDataSetup = true;
            }

            if (VE2CoreServiceLocator.Instance.PlayerSettingsProvider == null) 
            {
                Debug.LogError("Error, V_PlayerSpawner cannot spawn player, no player settings provider found");
                return;
            }

            if (enableVR)
                StartCoroutine(InitializeXR());

            //TODO, maybe we have the service in charge of this async stuff, then its easier to test
            //Although, the service is doing a lot already, the integration maybe isn't the worst place for this..
            //its not like we CAN'T test a Monobehaviour, we can just mock the service locator singleton
            if (VE2CoreServiceLocator.Instance.PlayerSettingsProvider.ArePlayerSettingsReady)
                HandlePlayerSettingsReady();
            else
                VE2CoreServiceLocator.Instance.PlayerSettingsProvider.OnPlayerSettingsReady += HandlePlayerSettingsReady; //TODO- Maybe just wire this into the PlayerService?
        }

        private IEnumerator InitializeXR()
        {
            yield return VE2CoreServiceLocator.Instance.XRManagerWrapper.InitializeLoader();

            if (VE2CoreServiceLocator.Instance.XRManagerWrapper.ActiveLoader == null)
            {
                Debug.LogError("Failed to initialize XR Loader.");
            }
            else
            {
                Debug.Log("XR initialized and subsystems started.");
                _xrInitialized = true;
            }
        }

        private void HandlePlayerSettingsReady()
        {
            VE2CoreServiceLocator.Instance.PlayerSettingsProvider.OnPlayerSettingsReady -= HandlePlayerSettingsReady;

            if (enableVR && !_xrInitialized)
                StartCoroutine(InitializePlayerServiceAfterXRInit());
            else
                InitializePlayerService();
        }

        private IEnumerator InitializePlayerServiceAfterXRInit()
        {
            while (!_xrInitialized)
                yield return null;

            InitializePlayerService();
        }

        private void InitializePlayerService()
        {
            _playerService = VE2PlayerServiceFactory.Create(_playerTransformData, playerStateConfig, enableVR, enable2D);
        }

        private void FixedUpdate() 
        {
            _playerService?.HandleFixedUpdate();
        }

        private void OnDisable() 
        {
            _playerService?.TearDown();

            if (enableVR && VE2CoreServiceLocator.Instance.XRManagerWrapper.IsInitializationComplete)
            {
                VE2CoreServiceLocator.Instance.XRManagerWrapper.StopSubsystems();
                VE2CoreServiceLocator.Instance.XRManagerWrapper.DeinitializeLoader();
            }
        }
    }
}
