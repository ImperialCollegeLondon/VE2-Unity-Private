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
            IXRManagerWrapper xrManagerWrapper, IPrimaryUIServiceInternal primaryUIService, ISecondaryUIServiceInternal secondaryUIService, IXRHapticsWrapper xRHapticsWrapperLeft, IXRHapticsWrapper xRHapticsWrapperRight)
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
                xRHapticsWrapperRight); //TODO: reorder these?
        }
    }

    internal class PlayerService : IPlayerService, IPlayerServiceInternal
    {
        #region Interfaces  
        public PlayerTransformData PlayerTransformData {get; private set;}

        public event Action<OverridableAvatarAppearance> OnOverridableAvatarAppearanceChanged;

        public void MarkPlayerSettingsUpdated() 
        {
            _playerSettingsHandler.MarkAppearanceChanged();
            //OnOverridableAvatarAppearanceChanged?.Invoke(OverridableAvatarAppearance);
        }

        public OverridableAvatarAppearance OverridableAvatarAppearance { 
            get 
            {
                return new OverridableAvatarAppearance(
                    _playerSettingsHandler.PlayerPresentationConfig,
                    _config.AvatarAppearanceOverrideConfig.OverrideHead,
                    _config.AvatarAppearanceOverrideConfig.HeadOverrideIndex, 
                    _config.AvatarAppearanceOverrideConfig.OverrideTorso,
                    _config.AvatarAppearanceOverrideConfig.TorsoOverrideIndex);
            } 
        }


        public bool RememberPlayerSettings { get => _playerSettingsHandler.RememberPlayerSettings; set => _playerSettingsHandler.RememberPlayerSettings = value; }

        public TransmissionProtocol TransmissionProtocol => _config.RepeatedTransmissionConfig.TransmissionType;
        public float TransmissionFrequency => _config.RepeatedTransmissionConfig.TransmissionFrequency;

        public bool IsVRMode => PlayerTransformData.IsVRMode;
        public UnityEvent OnChangeToVRMode => _config.PlayerModeConfig.OnChangeToVRMode;
        public UnityEvent OnChangeTo2DMode => _config.PlayerModeConfig.OnChangeTo2DMode;
        public UnityEvent OnTeleport => _config.MovementModeConfig.OnTeleport; 
        public UnityEvent OnSnapTurn => _config.MovementModeConfig.OnSnapTurn;
        public UnityEvent OnHorizontalDrag => _config.MovementModeConfig.OnHorizontalDrag;
        public UnityEvent OnVerticalDrag => _config.MovementModeConfig.OnVerticalDrag;
        public UnityEvent OnJump2D => _config.MovementModeConfig.OnJump2D;
        public UnityEvent OnCrouch2D => _config.MovementModeConfig.OnCrouch2D;
        public UnityEvent OnResetViewVR => _config.CameraConfig.OnResetViewVR;

        public List<GameObject> HeadOverrideGOs => _config.AvatarAppearanceOverrideConfig.HeadOverrideGameObjects;
        public List<GameObject> TorsoOverrideGOs => _config.AvatarAppearanceOverrideConfig.TorsoOverrideGameObjects;

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

        public void SetAvatarHeadOverride(ushort index) 
        {
            _config.AvatarAppearanceOverrideConfig.OverrideHead = true;
            _config.AvatarAppearanceOverrideConfig.HeadOverrideIndex = index;
            OnOverridableAvatarAppearanceChanged?.Invoke(OverridableAvatarAppearance);
        }
            
        public void SetAvatarTorsoOverride(ushort index) 
        {
            _config.AvatarAppearanceOverrideConfig.OverrideTorso = true;
            _config.AvatarAppearanceOverrideConfig.TorsoOverrideIndex = index;
            OnOverridableAvatarAppearanceChanged?.Invoke(OverridableAvatarAppearance);
        }

        public void ClearAvatarHeadOverride() 
        {
            _config.AvatarAppearanceOverrideConfig.OverrideHead = false;
            OnOverridableAvatarAppearanceChanged?.Invoke(OverridableAvatarAppearance);
        }
        public void ClearAvatarTorsoOverride()
        {
            _config.AvatarAppearanceOverrideConfig.OverrideTorso = false;
            OnOverridableAvatarAppearanceChanged?.Invoke(OverridableAvatarAppearance);
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
        
        public AndroidJavaObject AddArgsToIntent(AndroidJavaObject intent) => _playerSettingsHandler.AddArgsToIntent(intent);

        public void AddPanelTo2DOverlayUI(RectTransform rect) => _player2D.MoveRectToOverlayUI(rect);
        #endregion

        private readonly PlayerConfig _config;
        private readonly PlayerController2D _player2D;
        private readonly PlayerControllerVR _playerVR;

        private readonly PlayerInputContainer _playerInputContainer;
        private readonly IPlayerPersistentDataHandler _playerSettingsHandler;
        private readonly ILocalPlayerSyncableContainer _playerSyncContainer;
        private readonly IPrimaryUIServiceInternal _primaryUIService;

        internal PlayerService(PlayerTransformData transformData, PlayerConfig config, HandInteractorContainer interactorContainer, IPlayerPersistentDataHandler playerSettingsHandler, 
            ILocalClientIDWrapper localClientIDWrapper, ILocalAdminIndicator localAdminIndicator, ILocalPlayerSyncableContainer playerSyncContainer, IGrabInteractablesContainer grabInteractablesContainer, 
            PlayerInputContainer playerInputContainer, IRaycastProvider raycastProvider, ICollisionDetectorFactory collisionDetectorFactory, IXRManagerWrapper xrManagerWrapper, 
            IPrimaryUIServiceInternal primaryUIService, ISecondaryUIServiceInternal secondaryUIService, IXRHapticsWrapper xRHapticsWrapperLeft, IXRHapticsWrapper xRHapticsWrapperRight)
        {
            PlayerTransformData = transformData;
            _config = config;

            _playerInputContainer = playerInputContainer;
            _playerSettingsHandler = playerSettingsHandler;

            _playerSyncContainer = playerSyncContainer;
            _playerSyncContainer.RegisterLocalPlayer(this);

            if (_config.PlayerModeConfig.EnableVR)
            {
                xrManagerWrapper.InitializeLoader(); //TODO: might not need to do this if its already been init? E.G, don't do again on domain reload - could just put a flag in the wrapper?

                _playerVR = new PlayerControllerVR(
                    interactorContainer, grabInteractablesContainer, _playerInputContainer.PlayerVRInputContainer,
                    playerSettingsHandler, new PlayerVRControlConfig(), _config.PlayerInteractionConfig, _config.MovementModeConfig, _config.CameraConfig,
                    raycastProvider, collisionDetectorFactory, xrManagerWrapper, localClientIDWrapper, localAdminIndicator, primaryUIService, secondaryUIService, xRHapticsWrapperLeft, xRHapticsWrapperRight);
            }

            if (_config.PlayerModeConfig.Enable2D)
            {
                _player2D = new PlayerController2D(
                    interactorContainer, grabInteractablesContainer, _playerInputContainer.Player2DInputContainer,
                    playerSettingsHandler, new Player2DControlConfig(), _config.PlayerInteractionConfig, _config.MovementModeConfig, _config.CameraConfig,
                    raycastProvider, collisionDetectorFactory, localClientIDWrapper, localAdminIndicator, primaryUIService, secondaryUIService, this);
            }

            _playerSettingsHandler.OnDebugSaveAppearance += HandlePlayerPresentationChanged;
            HandlePlayerPresentationChanged(_playerSettingsHandler.PlayerPresentationConfig); //Do this now to set the initial appearance

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

        private void HandlePlayerPresentationChanged(PlayerPresentationConfig presentationConfig)
        {
            OnOverridableAvatarAppearanceChanged?.Invoke(OverridableAvatarAppearance);

            Color newCol = new Color(
                presentationConfig.AvatarRed,
                presentationConfig.AvatarGreen,
                presentationConfig.AvatarBlue) / 255f;

            //TODO - should the individual player controllers be in charge of this? 
            //We need to emit the event just from a single place, though
            _playerVR?.HandleLocalAvatarColorChanged(newCol);
            _player2D?.HandleReceiveAvatarAppearance(OverridableAvatarAppearance);
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
