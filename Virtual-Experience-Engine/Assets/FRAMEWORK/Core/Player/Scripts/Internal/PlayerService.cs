using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using VE2.Common.API;
using VE2.Common.Shared;
using VE2.Core.Player.API;
using VE2.Core.UI.API;
using static VE2.Core.Player.API.PlayerSerializables;

namespace VE2.Core.Player.Internal
{
    internal static class VE2PlayerServiceFactory
    {
        internal static PlayerService Create(PlayerTransformData state, PlayerConfig config, IPlayerPersistentDataHandler playerPersistentDataHandler, 
            IXRManagerWrapper xrManagerWrapper, IPrimaryUIServiceInternal primaryUIService, ISecondaryUIServiceInternal secondaryUIService, IXRHapticsWrapper xRHapticsWrapperLeft, IXRHapticsWrapper xRHapticsWrapperRight, ITransformWrapper playerSpawnTransform)
        {
            return new PlayerService(state, config,
                VE2API.InteractorContainer,
                playerPersistentDataHandler,
                VE2API.LocalClientIdWrapper,
                VE2API.LocalAdminIndicator,
                VE2API.LocalPlayerSyncableContainer,
                VE2API.GrabInteractablesContainer,
                VE2API.InputHandler.PlayerInputContainer,
                new RaycastProvider(),
                new CollisionDetectorFactory(),
                xrManagerWrapper,
                primaryUIService,
                secondaryUIService,
                xRHapticsWrapperLeft,
                xRHapticsWrapperRight,
                playerSpawnTransform); //TODO: reorder these?
        }
    }

    internal class PlayerService : IPlayerService, IPlayerServiceInternal
    {
        #region Plugin Interfaces  
        public bool IsVRMode => PlayerTransformData.IsVRMode;
        public UnityEvent OnChangeToVRMode => _config.PlayerModeConfig.OnChangeToVRMode;
        public UnityEvent OnChangeTo2DMode => _config.PlayerModeConfig.OnChangeTo2DMode;
        public UnityEvent OnTeleport => _config.MovementModeConfig.OnTeleport; 
        public UnityEvent OnSnapTurn => _config.MovementModeConfig.OnSnapTurn;
        public UnityEvent OnHorizontalDrag => _config.MovementModeConfig.OnHorizontalDrag;
        public UnityEvent OnVerticalDrag => _config.MovementModeConfig.OnVerticalDrag;
        //public UnityEvent OnFreeFlyModeEnter => _config.MovementModeConfig.OnFreeFlyModeEnter;
        //public UnityEvent OnFreeFlyModeExit => _config.MovementModeConfig.OnFreeFlyModeExit;
        public UnityEvent OnJump2D => _config.MovementModeConfig.OnJump2D;
        public UnityEvent OnCrouch2D => _config.MovementModeConfig.OnCrouch2D;
        //public UnityEvent OnFreeFlyModeEnter2D => _config.MovementModeConfig.OnFreeFlyModeEnter2D;
        //public UnityEvent OnFreeFlyModeExit2D => _config.MovementModeConfig.OnFreeFlyModeExit2D;
        public UnityEvent OnResetViewVR => _config.CameraConfig.OnResetViewVR;

        public Camera ActiveCamera
        {
            get
            {
                if (PlayerTransformData.IsVRMode)
                    return _playerVR.Camera;
                else
                    return _player2D.Camera;
            }
        }

        public Vector3 PlayerSpawnPoint => _playerSpawnTransform.position;

        
        public void SetBuiltInHeadEnabled(bool isEnabled)
        {
            _config.PluginAvatarSelections.HeadGameObjectSelection.BuiltInGameObjectEnabled = isEnabled;
            MarkPlayerAvatarChanged();
        }

        public void SetCustomHeadEnabled(bool isEnabled)
        {
            _config.PluginAvatarSelections.HeadGameObjectSelection.CustomGameObjectEnabled = isEnabled;
            MarkPlayerAvatarChanged();
        }

        public void SetCustomHeadIndex(ushort type)
        {
            _config.PluginAvatarSelections.HeadGameObjectSelection.CustomGameObjectIndex = type;
            MarkPlayerAvatarChanged();
        }

        public void SetBuiltInTorsoEnabled(bool isEnabled)
        {
            _config.PluginAvatarSelections.TorsoGameObjectSelection.BuiltInGameObjectEnabled = isEnabled;
            MarkPlayerAvatarChanged();
        }

        public void SetCustomTorsoEnabled(bool isEnabled)
        {
            _config.PluginAvatarSelections.TorsoGameObjectSelection.CustomGameObjectEnabled = isEnabled;
            MarkPlayerAvatarChanged();
        }

        public void SetCustomTorsoIndex(ushort type)
        {
            _config.PluginAvatarSelections.TorsoGameObjectSelection.CustomGameObjectIndex = type;
            MarkPlayerAvatarChanged();
        }

        public void SetBuiltInRightHandVREnabled(bool isEnabled)
        {
            _config.PluginAvatarSelections.RightHandVRGameObjectSelection.BuiltInGameObjectEnabled = isEnabled;
            MarkPlayerAvatarChanged();
        }

        public void SetCustomRightHandVREnabled(bool isEnabled)
        {
            _config.PluginAvatarSelections.RightHandVRGameObjectSelection.CustomGameObjectEnabled = isEnabled;
            MarkPlayerAvatarChanged();
        }

        public void SetCustomRightHandVRIndex(ushort type)
        {
            _config.PluginAvatarSelections.RightHandVRGameObjectSelection.CustomGameObjectIndex = type;
            MarkPlayerAvatarChanged();
        }

        public void SetBuiltInLeftHandVREnabled(bool isEnabled)
        {
            _config.PluginAvatarSelections.LeftHandVRGameObjectSelection.BuiltInGameObjectEnabled = isEnabled;
            MarkPlayerAvatarChanged();
        }

        public void SetCustomLeftHandVREnabled(bool isEnabled)
        {
            _config.PluginAvatarSelections.LeftHandVRGameObjectSelection.CustomGameObjectEnabled = isEnabled;
            MarkPlayerAvatarChanged();
        }

        public void SetCustomLeftHandVRIndex(ushort type)
        {
            _config.PluginAvatarSelections.LeftHandVRGameObjectSelection.CustomGameObjectIndex = type;
            MarkPlayerAvatarChanged();
        }

        private void MarkPlayerAvatarChanged()
        {
            _playerSettingsHandler.SaveAppearance();
            _activeAvatarHandler.UpdateInstancedAvatarAppearance(InstancedAvatarAppearance);
            OnInstancedAvatarAppearanceChanged?.Invoke(InstancedAvatarAppearance);
        }

        public Vector3 PlayerPosition => PlayerTransformData.IsVRMode ? _playerVR.PlayerPosition : _player2D.PlayerPosition;

        public void SetPlayerPosition(Vector3 position)
        {
            if (PlayerTransformData.IsVRMode)
                _playerVR.SetPlayerPosition(position);
            else
                _player2D.SetPlayerPosition(position);
        }

        public Quaternion PlayerRotation => PlayerTransformData.IsVRMode ? _playerVR.PlayerRotation : _player2D.PlayerRotation;
        public void SetPlayerRotation(Quaternion rotation)
        {
            if (PlayerTransformData.IsVRMode)
                _playerVR.SetPlayerRotation(rotation);
            else
                _player2D.SetPlayerRotation(rotation);
        }
        public void ToggleFreeFlyMode(bool toggle) => _config.MovementModeConfig.FreeFlyMode = toggle;
        #endregion

        #region Internal Interface

        public PlayerTransformData PlayerTransformData {get; private set;}

        public event Action<InstancedAvatarAppearance> OnInstancedAvatarAppearanceChanged;
        public InstancedAvatarAppearance InstancedAvatarAppearance => new(_playerSettingsHandler.BuiltInPlayerGameObjectConfig, _config.PluginAvatarSelections);

        public bool RememberPlayerSettings { get => _playerSettingsHandler.RememberPlayerSettings; set => _playerSettingsHandler.RememberPlayerSettings = value; }

        public TransmissionProtocol TransmissionProtocol => _config.RepeatedTransmissionConfig.TransmissionType;
        public float TransmissionFrequency => _config.RepeatedTransmissionConfig.TransmissionFrequency;

        public void SetBuiltInHeadIndex(ushort type)
        {
            _playerSettingsHandler.BuiltInPlayerGameObjectConfig.AvatarHeadIndex = type;
            MarkPlayerAvatarChanged();
        }

        public void SetBuiltInTorsoIndex(ushort type)
        {
            _playerSettingsHandler.BuiltInPlayerGameObjectConfig.AvatarTorsoIndex = type;
            MarkPlayerAvatarChanged();
        }

        public void SetBuiltInColor(Color color)
        {
            _playerSettingsHandler.BuiltInPlayerGameObjectConfig.AvatarColor = color;
            MarkPlayerAvatarChanged();
        }

        public AvatarPrefabs BuiltInGameObjectPrefabs { get; private set; }
        public AvatarPrefabs CustomGameObjectPrefabs => _config.PluginCustomAvatarPrefabs;

        public AndroidJavaObject AddArgsToIntent(AndroidJavaObject intent) => _playerSettingsHandler.AddArgsToIntent(intent);

        public void AddPanelTo2DOverlayUI(RectTransform rect) => _player2D.MoveRectToOverlayUI(rect);

        public Collider CharacterCollider2D => _player2D != null? _player2D.CharacterCollider : null;
        #endregion

        private readonly PlayerConfig _config;
        private readonly PlayerController2D _player2D;
        private readonly PlayerControllerVR _playerVR;

        private PlayerAvatarHandler _activeAvatarHandler => PlayerTransformData.IsVRMode ? _playerVR.AvatarHandler : _player2D.AvatarHandler;

        private readonly PlayerInputContainer _playerInputContainer;
        private readonly IPlayerPersistentDataHandler _playerSettingsHandler;
        private readonly ILocalPlayerSyncableContainer _playerSyncContainer;
        private readonly IPrimaryUIServiceInternal _primaryUIService;
        private readonly ITransformWrapper _playerSpawnTransform;

        internal PlayerService(PlayerTransformData transformData, PlayerConfig config, HandInteractorContainer interactorContainer, IPlayerPersistentDataHandler playerSettingsHandler, 
            ILocalClientIDWrapper localClientIDWrapper, ILocalAdminIndicator localAdminIndicator, ILocalPlayerSyncableContainer playerSyncContainer, IGrabInteractablesContainer grabInteractablesContainer, 
            PlayerInputContainer playerInputContainer, IRaycastProvider raycastProvider, ICollisionDetectorFactory collisionDetectorFactory, IXRManagerWrapper xrManagerWrapper, 
            IPrimaryUIServiceInternal primaryUIService, ISecondaryUIServiceInternal secondaryUIService, IXRHapticsWrapper xRHapticsWrapperLeft, IXRHapticsWrapper xRHapticsWrapperRight, ITransformWrapper playerSpawnTransform)
        {
            PlayerTransformData = transformData;
            _config = config;

            _playerInputContainer = playerInputContainer;
            _playerSettingsHandler = playerSettingsHandler;

            List<GameObject> builtInHeadGameObjectPrefabs = new List<GameObject>()
            {
                Resources.Load<GameObject>("Avatars/Heads/V_Avatar_Head_Default_1"),
                Resources.Load<GameObject>("Avatars/Heads/V_Avatar_Head_Default_2"),
            };

            List<GameObject> builtInTorsoGameObjectPrefabs = new List<GameObject>()
            {
                Resources.Load<GameObject>("Avatars/Torsos/V_Avatar_Torso_Default_1"),
                Resources.Load<GameObject>("Avatars/Torsos/V_Avatar_Torso_Default_2")
            };

            List<GameObject> builtInHandVRGameObjectPrefabs = new List<GameObject>()
            {
                Resources.Load<GameObject>("Avatars/VRHands/V_Avatar_VRLeftHand_Default_1"),
            };

            BuiltInGameObjectPrefabs = new(builtInHeadGameObjectPrefabs, builtInTorsoGameObjectPrefabs, builtInHandVRGameObjectPrefabs);
            AvatarHandlerBuilderContext avatarHandlerBuilderContext = new(BuiltInGameObjectPrefabs, config.PluginCustomAvatarPrefabs, InstancedAvatarAppearance);

            _playerSyncContainer = playerSyncContainer;
            _playerSyncContainer.RegisterLocalPlayer(this);

            _playerSpawnTransform = playerSpawnTransform;

            if (_config.PlayerModeConfig.EnableVR)
            {
                xrManagerWrapper.InitializeLoader(); //TODO: might not need to do this if its already been init? E.G, don't do again on domain reload - could just put a flag in the wrapper?

                _playerVR = new PlayerControllerVR(
                    interactorContainer, grabInteractablesContainer, _playerInputContainer.PlayerVRInputContainer,
                    playerSettingsHandler, avatarHandlerBuilderContext, new PlayerVRControlConfig(), _config.PlayerInteractionConfig, _config.MovementModeConfig, _config.CameraConfig,
                    raycastProvider, collisionDetectorFactory, xrManagerWrapper, localClientIDWrapper, localAdminIndicator, primaryUIService, secondaryUIService, xRHapticsWrapperLeft, xRHapticsWrapperRight);
            }

            if (_config.PlayerModeConfig.Enable2D)
            {
                _player2D = new PlayerController2D(
                    interactorContainer, grabInteractablesContainer, _playerInputContainer.Player2DInputContainer,
                    playerSettingsHandler, avatarHandlerBuilderContext, new Player2DControlConfig(), _config.PlayerInteractionConfig, _config.MovementModeConfig, _config.CameraConfig,
                    raycastProvider, collisionDetectorFactory, localClientIDWrapper, localAdminIndicator, primaryUIService, secondaryUIService, this);
            }

            if (_config.PlayerModeConfig.EnableVR && !_config.PlayerModeConfig.Enable2D)
                PlayerTransformData.IsVRMode = true;
            else if (_config.PlayerModeConfig.Enable2D && !_config.PlayerModeConfig.EnableVR)
                PlayerTransformData.IsVRMode = false;
            else if (Application.isPlaying && playerSettingsHandler.PersistentPlayerMode != PersistentPlayerMode.NotInitialized) //Only if playing, so we don't break tests
                PlayerTransformData.IsVRMode = playerSettingsHandler.PersistentPlayerMode == PersistentPlayerMode.VR;

            _playerSettingsHandler.PersistentPlayerMode = PlayerTransformData.IsVRMode ? PersistentPlayerMode.VR : PersistentPlayerMode.TwoD;

            if (PlayerTransformData.IsVRMode)
                _playerVR.ActivatePlayer(PlayerTransformData);
            else
                _player2D.ActivatePlayer(PlayerTransformData);

            _playerInputContainer.ChangeMode.OnPressed += HandleChangeModePressed;

            _primaryUIService = primaryUIService;
            if (_primaryUIService != null)
                SetupUI();
        }

        private void SetupUI()
        {
            GameObject settingsUIHolder = GameObject.Instantiate(Resources.Load<GameObject>("PlayerSettingsUIHolder"));
            GameObject settingsUI = settingsUIHolder.transform.GetChild(0).gameObject;
            settingsUI.SetActive(false);
        
            _primaryUIService.AddNewTab("Settings", settingsUI, Resources.Load<Sprite>("PlayerSettingsUIIcon"), 2);
            GameObject.DestroyImmediate(settingsUIHolder);
            
            GameObject helpUIHolder = GameObject.Instantiate(Resources.Load<GameObject>("PlayerHelpUIHolder"));
            GameObject helpUI = helpUIHolder.transform.GetChild(0).gameObject;
            helpUI.SetActive(false);

            _primaryUIService.AddNewTab("Help", helpUI, Resources.Load<Sprite>("PlayerHelpUIIcon"), 3);
            GameObject.DestroyImmediate(helpUIHolder);

            if (_config.PlayerModeConfig.Enable2D && _config.PlayerModeConfig.EnableVR)
                _primaryUIService.EnableModeSwitchButtons();

            _primaryUIService.OnSwitchTo2DButtonClicked += () => HandleChangeModePressed();
            _primaryUIService.OnSwitchToVRButtonClicked += () => HandleChangeModePressed();

            if (PlayerTransformData.IsVRMode)
                _primaryUIService.ShowSwitchTo2DButton();
            else 
                _primaryUIService.ShowSwitchToVRButton();
        }

        private void HandleChangeModePressed() 
        {
            if (!_config.PlayerModeConfig.Enable2D || !_config.PlayerModeConfig.EnableVR)
                return; //Can't change modes if both aren't enabled!

            try
            {
                if (PlayerTransformData.IsVRMode) //switch to 2d
                {
                    _playerVR.DeactivatePlayer();
                    _player2D.ActivatePlayer(PlayerTransformData);

                    if (_primaryUIService != null)
                        _primaryUIService.ShowSwitchToVRButton();
                }
                else //switch to vr
                {
                    _player2D.DeactivatePlayer();
                    _playerVR.ActivatePlayer(PlayerTransformData);

                    if (_primaryUIService != null)
                        _primaryUIService.ShowSwitchTo2DButton();
                }

                PlayerTransformData.IsVRMode = !PlayerTransformData.IsVRMode;
                _playerSettingsHandler.PersistentPlayerMode = PlayerTransformData.IsVRMode ? PersistentPlayerMode.VR : PersistentPlayerMode.TwoD;
            }
            catch (System.Exception e)
            {
                Debug.LogError("Error changing player mode: " + e.Message + " - " + e.StackTrace);
            }

            try 
            {
                if (PlayerTransformData.IsVRMode)
                    OnChangeToVRMode?.Invoke();
                else 
                    OnChangeTo2DMode?.Invoke();
            }
            catch (System.Exception e)
            {
                Debug.LogError("Error emitting OnChangeToVRMode or OnChangeTo2DMode: " + e.Message + " - " + e.StackTrace);
            }
        }

        public void HandleFixedUpdate()
        {
            //Note: Subcontrollers return new instances of PTD, breaking the reference to the one serialized at the MB level 
            //This means the MB has to manually update its serialized field with the new data
            if (PlayerTransformData.IsVRMode)
                PlayerTransformData = _playerVR.PlayerTransformData;
            else
                PlayerTransformData = _player2D.PlayerTransformData;                
        }

        public void HandleUpdate()
        {
            if (PlayerTransformData.IsVRMode)
                _playerVR.HandleUpdate();
            else
                _player2D.HandleUpdate();

        }
        
        public void TearDown() 
        {
            _playerSyncContainer.DeregisterLocalPlayer();

            //TODO - maybe make these TearDown methods instead?
            if (_player2D != null)
            {
                _player2D.DeactivatePlayer();
            }

            if (_playerVR != null)
            {
                _playerVR.DeactivatePlayer();
                _playerVR.TearDown();
            }

            if (_primaryUIService != null)
            {
                _primaryUIService.OnSwitchTo2DButtonClicked -= HandleChangeModePressed;
                _primaryUIService.OnSwitchToVRButtonClicked -= HandleChangeModePressed;
            }

            _playerInputContainer.ChangeMode.OnPressed -= HandleChangeModePressed;
        }
    }
}
