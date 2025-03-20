using System;
using System.Collections.Generic;
using UnityEngine;
using VE2.Core.Common;
using VE2.Core.Player.API;
using VE2.Core.UI.API;
using static VE2.Core.Player.API.PlayerSerializables;

namespace VE2.Core.Player.Internal
{
    [Serializable]
    internal class PlayerConfig
    {
        [SerializeField] public bool EnableVR = false;
        [SerializeField] public bool Enable2D = true;

        [Title("Movement Mode Config")]
        [BeginGroup(Style = GroupStyle.Round), SerializeField, IgnoreParent, EndGroup] public MovementModeConfig MovementModeConfig = new();


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
        [SpaceArea(spaceAfter: 10), BeginGroup(Style = GroupStyle.Round, ApplyCondition = true), EndGroup(ApplyCondition = true), SerializeField, IgnoreParent] public RepeatedTransmissionConfig RepeatedTransmissionConfig = new(TransmissionProtocol.UDP, 35);
        
        private bool _hasMultiplayerSupport => PlayerAPI.HasMultiPlayerSupport;
    }

    [Serializable]
    internal class MovementModeConfig
    {
        [SerializeField] internal LayerMask TraversableLayers; 
        [SerializeField] internal bool EnableFreeFlyMode = false;
        [SerializeField] internal float TeleportRangeMultiplier = 1.0f;
    }

    [ExecuteAlways]
    internal class V_PlayerSpawner : MonoBehaviour, IPlayerServiceProvider
    {
        //TODO, configs for each player, OnTeleport, DragHeight, FreeFlyMode, etc
        [SerializeField, IgnoreParent] private PlayerConfig _playerConfig = new();

        [SpaceArea(spaceBefore: 10), Help("If running standalone, this presentation config will be used, if integrated with the VE2 platform, the platform will provide the presentation config.")]
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

        [SerializeField, HideInInspector] private bool _transformDataSetup = false;
        [SerializeField, HideInInspector] private PlayerTransformData _playerTransformData = new();
        //[SerializeField, HideInInspector] private GameObject _playerPreview;
        private string _playerPreviewName = "VE2PlayerPreviewVisualisation";
        private GameObject _playerPreview => GameObject.Find(_playerPreviewName); //Can't just store GO reference, as that'll get wiped as the inspector resets

        private void Reset()
        {
            _playerConfig = new();
            _playerConfig.MovementModeConfig.TraversableLayers = LayerMask.GetMask("Ground"); //Can't set LayerMask in serialization, so we do it here

            //Debug.Log("Resetting - " + (_playerPreview != null));
            if (_playerPreview != null)
                DestroyImmediate(_playerPreview);
                
            CreatePlayerPreview();
        }

        private void OnEnable() 
        {
            PlayerAPI.PlayerServiceProvider = this;

            if (!Application.isPlaying || _playerService != null)
                return;

            _playerPreview?.SetActive(false);

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
                _playerConfig.EnableVR = true;
                _playerConfig.Enable2D = false;
            }

            XRManagerWrapper xrManagerWrapper = FindFirstObjectByType<XRManagerWrapper>();
            if (xrManagerWrapper == null)
                xrManagerWrapper = new GameObject("XRManagerWrapper").AddComponent<XRManagerWrapper>();

            //May be null if UIs aren't available
            IPrimaryUIServiceInternal primaryUIService = UIAPI.PrimaryUIService as IPrimaryUIServiceInternal;
            ISecondaryUIServiceInternal secondaryUIService = UIAPI.SecondaryUIService as ISecondaryUIServiceInternal;

            _playerService = VE2PlayerServiceFactory.Create(
                _playerTransformData, 
                _playerConfig, 
                playerPersistentDataHandler,
                xrManagerWrapper,
                primaryUIService,
                secondaryUIService);
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

        private void OnDestroy()
        {
            if (_playerPreview != null)
                DestroyImmediate(_playerPreview);
        }

        private void CreatePlayerPreview()
        {
            GameObject playerPreview = GameObject.Instantiate<GameObject>(Resources.Load<GameObject>(_playerPreviewName));
            playerPreview.name = _playerPreviewName;
            playerPreview.transform.SetParent(transform);
            playerPreview.transform.localPosition = Vector3.zero;
            playerPreview.transform.localRotation = Quaternion.identity;

            foreach (Transform child in playerPreview.GetComponentsInChildren<Transform>(true))
                child.gameObject.hideFlags = HideFlags.HideInHierarchy | HideFlags.NotEditable;
        }

// #if UNITY_EDITOR
//         private void OnSelectionChanged()
//         {
//             if (_playerPreview == null) 
//                 return;

//             // Check if the selected object is the target or a child of it
//             foreach (var selected in UnityEditor.Selection.gameObjects)
//             {
//                 if (IsChildOrSelf(_playerPreview, selected))
//                 {
//                     UnityEditor.Selection.activeGameObject = gameObject;
//                     break;
//                 }
//             }
//         }

//         private bool IsChildOrSelf(GameObject parent, GameObject obj) => obj == parent || obj.transform.IsChildOf(parent.transform);
//         #endif
    }
}
