using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering.Universal;
using VE2.Common.API;
using VE2.Common.Shared;
using VE2.Core.Player.API;
using VE2.Core.UI.API;
using static VE2.Core.Player.API.PlayerSerializables;

namespace VE2.Core.Player.Internal
{

    /*
        TODO - when attaching player spawner, deleting, and then undoing, we are missing the player preview 
        This is likely also true for UI provider
    */

    [Serializable]
    internal enum SupportedPlayerModes
    {
        OnlyVR,
        Only2D,
        Both
    }

    [Serializable]
    internal class PlayerConfig
    {
        [Title("Player Mode Config")]
        [BeginGroup(Style = GroupStyle.Round), SerializeField, IgnoreParent, EndGroup] public PlayerModeConfig PlayerModeConfig = new();

        [Title("Interaction Config")]
        [BeginGroup(Style = GroupStyle.Round), SerializeField, IgnoreParent, EndGroup] public PlayerInteractionConfig PlayerInteractionConfig = new();

        [Title("Movement Mode Config")]
        [BeginGroup(Style = GroupStyle.Round), SerializeField, IgnoreParent, EndGroup] public MovementModeConfig MovementModeConfig = new();

        [Title("Camera Config")]
        [BeginGroup(Style = GroupStyle.Round), SerializeField, EndGroup] public CameraConfig CameraConfig = new();

        [Title("Avatar Appearance Overrides")]
        [BeginGroup(Style = GroupStyle.Round), SerializeField, IgnoreParent, EndGroup] public AvatarAppearanceOverrideConfig AvatarAppearanceOverrideConfig = new();

        [Title("Transmission Settings", ApplyCondition = true)]
        [HideIf(nameof(_hasMultiplayerSupport), false)]
        [SpaceArea(spaceAfter: 10), BeginGroup(Style = GroupStyle.Round, ApplyCondition = true), EndGroup(ApplyCondition = true), SerializeField, IgnoreParent] public PlayerTransmissionConfig RepeatedTransmissionConfig = new(TransmissionProtocol.UDP, 35);
        
        private bool _hasMultiplayerSupport => VE2API.HasMultiPlayerSupport;
    }

    [Serializable]
    internal class PlayerModeConfig
    {
        [SerializeField] internal SupportedPlayerModes SupportedPlayerModes = SupportedPlayerModes.Both;
        internal bool EnableVR => SupportedPlayerModes == SupportedPlayerModes.Both || SupportedPlayerModes == SupportedPlayerModes.OnlyVR;
        internal bool Enable2D => SupportedPlayerModes == SupportedPlayerModes.Both || SupportedPlayerModes == SupportedPlayerModes.Only2D;

        [SerializeField] internal UnityEvent OnChangeToVRMode;
        [SerializeField] internal UnityEvent OnChangeTo2DMode;
    }

    [Serializable]
    internal class PlayerInteractionConfig
    {
        [Tooltip("Player raycasts will hit objects on these layers, hands and feet will interact with interactables on these layers. Doesn't effect movement")]
        [SerializeField] internal LayerMask InteractableLayers;
    }

    [Serializable]
    internal class MovementModeConfig
    {
        [SerializeField] internal LayerMask TraversableLayers; 
        [SerializeField] internal LayerMask CollisionLayers;
        [SerializeField] internal bool FreeFlyMode = false;
        [SerializeField] internal float TeleportRangeMultiplier = 1.0f;
        [SerializeField] internal UnityEvent OnTeleport = new UnityEvent();
        [SerializeField] internal UnityEvent OnSnapTurn = new UnityEvent();
        [SerializeField] internal UnityEvent OnHorizontalDrag = new UnityEvent();
        [SerializeField] internal UnityEvent OnVerticalDrag = new UnityEvent();
        [SerializeField] internal UnityEvent OnJump2D = new UnityEvent();
        [SerializeField] internal UnityEvent OnCrouch2D = new UnityEvent();
    }

    [Serializable]
    internal class CameraConfig 
    {
        [SerializeField] internal float FieldOfView2D = 60f;
        [SerializeField] internal float NearClippingPlane = 0.15f;
        [SerializeField] internal float FarClippingPlane = 1000f;
        [SerializeField] internal LayerMask CullingMask;
        [SerializeField] internal AntialiasingMode AntiAliasing = AntialiasingMode.FastApproximateAntialiasing;
        [SerializeField, ShowIf(nameof(_showAAQuality), true)] internal AntialiasingQuality AntiAliasingQuality = AntialiasingQuality.Medium;
        private bool _showAAQuality => AntiAliasing == AntialiasingMode.SubpixelMorphologicalAntiAliasing || AntiAliasing == AntialiasingMode.TemporalAntiAliasing;
        [SerializeField] internal bool EnablePostProcessing = true;
        [SerializeField] internal bool OcclusionCulling = true;
        [SerializeField] internal UnityEvent OnResetViewVR = new UnityEvent();
    }

    [Serializable]
    internal class AvatarAppearanceOverrideConfig
    {
        [SerializeField] internal bool OverrideHead = false;
        [SerializeField, EnableIf(nameof(OverrideHead), true)] internal ushort HeadOverrideIndex = 0;
        [SerializeField, ReorderableList] internal List<GameObject> HeadOverrideGameObjects = new();

        [SerializeField] internal bool OverrideTorso = false;
        [SerializeField, EnableIf(nameof(OverrideTorso), true)] internal ushort TorsoOverrideIndex = 0;
        [SerializeField, ReorderableList] internal List<GameObject> TorsoOverrideGameObjects = new();
    }

    [Serializable]
    internal class PlayerTransmissionConfig 
    {
        [Suffix("Hz")]
        [Range(0.2f, 50f)]
        [SerializeField] public float TransmissionFrequency = 1;

        [SerializeField] public TransmissionProtocol TransmissionType;

        public PlayerTransmissionConfig(TransmissionProtocol transmissionType, float transmissionFrequency)
        {
            TransmissionType = transmissionType;
            TransmissionFrequency = transmissionFrequency;
        }

        protected virtual void OnValidate() //TODO - OnVlidate needs to come from VC
        {
            if (TransmissionFrequency > 1)
                TransmissionFrequency = Mathf.RoundToInt(TransmissionFrequency);
        }
    }

    [ExecuteAlways]
    internal class V_PlayerSpawner : MonoBehaviour, IPlayerServiceProvider
    {
        //TODO, configs for each player, OnTeleport, DragHeight, FreeFlyMode, etc
        [SerializeField, IgnoreParent] internal PlayerConfig _playerConfig = new();

        [SpaceArea(spaceBefore: 10), Help("If running standalone, this presentation config will be used, if integrated with the VE2 platform, the platform will provide the presentation config.")]
        [BeginGroup("Debug settings"), SerializeField, DisableInPlayMode, EndGroup]  private PlayerPresentationConfig _defaultPlayerPresentationConfig = new();

        #region Provider Interfaces
        private PlayerService _playerService;
        public IPlayerService PlayerService { 
            get 
            {
                if (_playerService == null)
                    OnEnable();

                return _playerService;
            }
        }

        public bool IsEnabled => this != null && gameObject != null && enabled && gameObject.activeInHierarchy;
        #endregion

        [SerializeField, HideInInspector] private bool _transformDataSetup = false;
        [SerializeField, HideInInspector] private PlayerTransformData _playerTransformData = new();
        private bool _editorListenersSetup = false;

        private GameObject _playerPreview => FindFirstObjectByType<PlayerPreviewTag>(FindObjectsInactive.Include)?.gameObject; //Can't just store GO reference, as that'll get wiped as the inspector resets

        private void Reset()
        {
            _playerConfig = new();

            //Can't set LayerMask in serialization, so we do it here
            _playerConfig.PlayerInteractionConfig.InteractableLayers = -1;
            _playerConfig.MovementModeConfig.TraversableLayers = LayerMask.GetMask("Ground");
            _playerConfig.MovementModeConfig.CollisionLayers = LayerMask.GetMask("Default"); 
            _playerConfig.CameraConfig.CullingMask = -1;

            //Debug.Log("Resetting - " + (_playerPreview != null));
            if (_playerPreview != null)
                DestroyImmediate(_playerPreview);
                
            CreatePlayerPreview();
        }

        private void OnEnable() 
        {
            #if UNITY_EDITOR
            if (!Application.isPlaying && !_editorListenersSetup)
            {
                _editorListenersSetup = true;
                UnityEditor.Selection.selectionChanged += OnSelectionChanged;
            }
            #endif

            VE2API.PlayerServiceProvider = this;

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

                #if UNITY_EDITOR
                if (_playerConfig.PlayerModeConfig.SupportedPlayerModes == SupportedPlayerModes.Both)
                    _playerTransformData.IsVRMode = VE2API.PreferVRMode;
                #endif
            }

            if (Application.platform == RuntimePlatform.Android && !Application.isEditor)
                _playerConfig.PlayerModeConfig.SupportedPlayerModes = SupportedPlayerModes.OnlyVR;

            XRManagerWrapper xrManagerWrapper = FindFirstObjectByType<XRManagerWrapper>();
            if (xrManagerWrapper == null)
                xrManagerWrapper = new GameObject("XRManagerWrapper").AddComponent<XRManagerWrapper>();

            XRHapticsWrapper xRHapticsWrapperLeft = new(true);
            XRHapticsWrapper xRHapticsWrapperRight = new(false);

            //May be null if UIs aren't available
            IPrimaryUIServiceInternal primaryUIService = VE2API.PrimaryUIService as IPrimaryUIServiceInternal;
            ISecondaryUIServiceInternal secondaryUIService = VE2API.SecondaryUIService as ISecondaryUIServiceInternal;

            _playerService = VE2PlayerServiceFactory.Create(
                _playerTransformData, 
                _playerConfig, 
                playerPersistentDataHandler,
                xrManagerWrapper,
                primaryUIService,
                secondaryUIService,
                xRHapticsWrapperLeft,
                xRHapticsWrapperRight);
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
            #if UNITY_EDITOR
            if (!Application.isPlaying && _editorListenersSetup)
            {
                _editorListenersSetup = false;
                UnityEditor.Selection.selectionChanged -= OnSelectionChanged;
            }
            #endif

            if (!Application.isPlaying)
                return;
            _playerService?.TearDown();
            _playerService = null;
        }

        private void CreatePlayerPreview()
        {
#if UNITY_EDITOR
            GameObject playerPreview = GameObject.Instantiate<GameObject>(Resources.Load<GameObject>("VE2PlayerPreviewVisualisation"));
            playerPreview.transform.SetParent(transform);
            playerPreview.transform.localPosition = Vector3.zero;
            playerPreview.transform.localRotation = Quaternion.identity;

            foreach (Transform child in playerPreview.GetComponentsInChildren<Transform>(true))
            {
                child.hideFlags = HideFlags.HideInHierarchy; // Keep it hidden
                UnityEditor.SceneVisibilityManager.instance.EnablePicking(child.gameObject, true); // Allow clicking in Scene view   
            }
            #endif
        }   

#if UNITY_EDITOR
        private void OnSelectionChanged()
        {
            if (_playerPreview == null) 
                return;

            // Check if the selected object is the target or a child of it
            foreach (var selected in UnityEditor.Selection.gameObjects)
            {
                if (IsChildOrSelf(_playerPreview, selected))
                {
                    UnityEditor.EditorApplication.delayCall += () => UnityEditor.Selection.activeGameObject = gameObject;
                    UnityEditor.EditorApplication.delayCall += () => UnityEditor.EditorApplication.RepaintHierarchyWindow();
                    break;
                }
            }
        }

        private bool IsChildOrSelf(GameObject parent, GameObject obj) => obj == parent || obj.transform.IsChildOf(parent.transform);
#endif
    }
}
