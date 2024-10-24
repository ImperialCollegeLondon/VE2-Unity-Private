using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.PlayerLoop;
using ViRSE.Core.Shared;
using static ViRSE.Core.Shared.CoreCommonSerializables;

namespace ViRSE.Core.Player
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

        [SerializeField, HideInInspector] private bool _transformDataSetup = false;
        [SerializeField, HideInInspector] private PlayerTransformData _playerTransformData = new();

        private ViRSEPlayerService _playerService;

        public void OnEnable() //TODO - hide this!
        {
            if (!_transformDataSetup)
            {
                _playerTransformData.RootPosition = transform.position;
                _playerTransformData.RootRotation = transform.rotation;
                _transformDataSetup = true;
            }

            if (ViRSECoreServiceLocator.Instance.PlayerSettingsProvider == null) 
            {
                Debug.LogError("Error, V_PlayerSpawner cannot spawn player, no player settings provider found");
                return;
            }

            //TODO, maybe we have the service in charge of this async stuff, then its easier to test
            //Although, the service is doing a lot already, the integration maybe isn't the worst place for this..
            //its not like we CAN'T test a Monobehaviour, we can just mock the service locator singleton
            if (ViRSECoreServiceLocator.Instance.PlayerSettingsProvider.ArePlayerSettingsReady)
                HandlePlayerSettingsReady();
            else
                ViRSECoreServiceLocator.Instance.PlayerSettingsProvider.OnPlayerSettingsReady += HandlePlayerSettingsReady; //TODO- Maybe just wire this into the PlayerService?
        }

        private void HandlePlayerSettingsReady()
        {
            ViRSECoreServiceLocator.Instance.PlayerSettingsProvider.OnPlayerSettingsReady -= HandlePlayerSettingsReady;
            _playerService = ViRSEPlayerServiceFactory.Create(_playerTransformData, playerStateConfig, enableVR, enable2D);
        }

        private void FixedUpdate() 
        {
            _playerService?.HandleFixedUpdate();
        }

        private void OnDisable() 
        {
            _playerService?.TearDown();
        }
    }
}
