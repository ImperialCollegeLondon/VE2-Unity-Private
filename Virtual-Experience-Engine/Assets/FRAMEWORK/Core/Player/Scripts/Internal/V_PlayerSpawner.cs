using System;
using System.Collections.Generic;
using UnityEngine;
using VE2.Core.Common;
using VE2.Core.Player.API;
using static VE2.Core.Player.API.PlayerSerializables;

namespace VE2.Core.Player.Internal
{
    [Serializable]
    internal class PlayerConfig
    {
        [SerializeField] public bool EnableVR = false;
        [SerializeField] public bool Enable2D = true;

        [Title("Avatar Presentation Override Selection")]
        [BeginGroup(Style = GroupStyle.Round), SerializeField] public AvatarAppearanceOverrideType HeadOverrideType = AvatarAppearanceOverrideType.None;
        [EndGroup, SerializeField] public AvatarAppearanceOverrideType TorsoOverrideType = AvatarAppearanceOverrideType.None;

        [Title("Head Overrides")]
        [BeginGroup(Style = GroupStyle.Round), SerializeField, AssetPreview] private GameObject HeadOverrideOne;
        [SerializeField, AssetPreview] private GameObject HeadOverrideTwo;
        [SerializeField, AssetPreview] private GameObject HeadOverrideThree;
        [SerializeField, AssetPreview] private GameObject HeadOverrideFour;
        [EndGroup, SerializeField, AssetPreview] private GameObject HeadOverrideFive;

        [Title("Torso Overrides")]
        [BeginGroup(Style = GroupStyle.Round), SerializeField, AssetPreview] private GameObject TorsoOverrideOne;
        [SerializeField, AssetPreview] private GameObject TorsoOverrideTwo;
        [SerializeField, AssetPreview] private GameObject TorsoOverrideThree;
        [SerializeField, AssetPreview] private GameObject TorsoOverrideFour;
        [EndGroup, SerializeField, AssetPreview] private GameObject TorsoOverrideFive;
        
        public List<GameObject> HeadOverrideGOs => new() { HeadOverrideOne, HeadOverrideTwo, HeadOverrideThree, HeadOverrideFour, HeadOverrideFive };
        public List<GameObject> TorsoOverrideGOs => new() { TorsoOverrideOne, TorsoOverrideTwo, TorsoOverrideThree, TorsoOverrideFour, TorsoOverrideFive }; 

        [Title("Transmission Settings", ApplyCondition = true)]
        [HideIf(nameof(_hasMultiplayerSupport), false)]
        [SpaceArea(spaceAfter: 10, Order = -1), BeginGroup(Style = GroupStyle.Round, ApplyCondition = true), EndGroup, SerializeField, IgnoreParent] public RepeatedTransmissionConfig RepeatedTransmissionConfig = new(TransmissionProtocol.UDP, 35);
        
        private bool _hasMultiplayerSupport => PlayerAPI.HasMultiPlayerSupport;
    }

    [ExecuteAlways]
    internal class V_PlayerSpawner : MonoBehaviour, IPlayerServiceProvider
    {
        //TODO, configs for each player, OnTeleport, DragHeight, FreeFlyMode, etc
        [SerializeField, IgnoreParent] public PlayerConfig playerConfig = new();

        [Help("If running standalone, this presentation config will be used, if integrated with the ViRSE platform, the platform will provide the presentation config.")]
        [BeginGroup("Debug settings"), SerializeField, DisableInPlayMode, EndGroup]  private PlayerPresentationConfig _defaultPlayerPresentationConfig = new();

        #region Provider Interfaces
        private PlayerService _playerService;
        public IPlayerService PlayerService { 
            get 
            {
                if (_playerService == null)
                {
                    OnEnable();
                    Debug.Log("Player service is null, re-enabling");   
                }


                return _playerService;
            }
        }

        public string GameObjectName { get => gameObject.name; }
        #endregion

        private bool _transformDataSetup = false;
        private PlayerTransformData _playerTransformData = new();

        private void OnEnable() 
        {
            PlayerAPI.PlayerServiceProvider = this;

            if (!Application.isPlaying || _playerService != null)
                return;

            PlayerPersistentDataHandler playerPersistentDataHandler = FindFirstObjectByType<PlayerPersistentDataHandler>();
            if (playerPersistentDataHandler == null)
            {
                playerPersistentDataHandler = new GameObject("PlayerPersisentDataHandler").AddComponent<PlayerPersistentDataHandler>();
                playerPersistentDataHandler.SetDefaults(_defaultPlayerPresentationConfig);
            }

            if (!_transformDataSetup)
            {
                _playerTransformData.RootPosition = transform.position;
                _playerTransformData.RootRotation = transform.rotation;
                _playerTransformData.VerticalOffset = 1.7f;
                _transformDataSetup = true;
            }

            if (Application.platform == RuntimePlatform.Android && !Application.isEditor)
            {
                playerConfig.EnableVR = true;
                playerConfig.Enable2D = false;
            }

            XRManagerWrapper xrManagerWrapper = FindFirstObjectByType<XRManagerWrapper>();
            if (xrManagerWrapper == null)
                xrManagerWrapper = new GameObject("XRManagerWrapper").AddComponent<XRManagerWrapper>();

            _playerService = VE2PlayerServiceFactory.Create(
                _playerTransformData, 
                playerConfig, 
                playerPersistentDataHandler,
                xrManagerWrapper);
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
                
            //Debug.Log("Disabling player spawner, service null? " + (_playerService == null));
            _playerService?.TearDown();
            _playerService = null;
        }
    }
}
