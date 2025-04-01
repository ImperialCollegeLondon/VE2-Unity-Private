using System;
using System.Collections.Generic;
using UnityEngine;
using VE2.Core.Common;
using VE2.Core.Player.API;
using VE2.Core.UI.API;
using VE2.Core.VComponents.API;
using static VE2.Core.Player.API.PlayerSerializables;

namespace VE2.Core.Player.Internal
{
    internal static class VE2PlayerServiceFactory
    {
        internal static PlayerService Create(PlayerTransformData state, PlayerConfig config, IPlayerPersistentDataHandler playerPersistentDataHandler, 
            IXRManagerWrapper xrManagerWrapper, IPrimaryUIServiceInternal primaryUIService, ISecondaryUIServiceInternal secondaryUIService)
        {
            return new PlayerService(state, config, 
            VComponentsAPI.InteractorContainer,
            playerPersistentDataHandler,
            PlayerAPI.LocalClientIDProvider,
            PlayerAPI.InputHandler.PlayerInputContainer,
            new RaycastProvider(),
            xrManagerWrapper,
            primaryUIService,
            secondaryUIService);
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
                    _config.HeadOverrideType, 
                    _config.TorsoOverrideType);
            } 
        }


        public bool RememberPlayerSettings { get => _playerSettingsHandler.RememberPlayerSettings; set => _playerSettingsHandler.RememberPlayerSettings = value; }

        public TransmissionProtocol TransmissionProtocol => _config.RepeatedTransmissionConfig.TransmissionType;
        public float TransmissionFrequency => _config.RepeatedTransmissionConfig.TransmissionFrequency;

        public bool IsVRMode => PlayerTransformData.IsVRMode;

        public List<GameObject> HeadOverrideGOs => _config.HeadOverrideGOs;
        public List<GameObject> TorsoOverrideGOs => _config.TorsoOverrideGOs;

        public void SetAvatarHeadOverride(AvatarAppearanceOverrideType type) 
        {
            _config.HeadOverrideType = type;
            OnOverridableAvatarAppearanceChanged?.Invoke(OverridableAvatarAppearance);
        }
            
        public void SetAvatarTorsoOverride(AvatarAppearanceOverrideType type) 
        {
            _config.TorsoOverrideType = type;
            OnOverridableAvatarAppearanceChanged?.Invoke(OverridableAvatarAppearance);
        }

        public AndroidJavaObject AddArgsToIntent(AndroidJavaObject intent) => _playerSettingsHandler.AddArgsToIntent(intent);

        public void AddPanelTo2DOverlayUI(RectTransform rect) => _player2D.MoveRectToOverlayUI(rect);
        #endregion

        private readonly PlayerConfig _config;
        private readonly PlayerController2D _player2D;
        private readonly PlayerControllerVR _playerVR;

        private readonly PlayerInputContainer _playerInputContainer;
        private readonly IPlayerPersistentDataHandler _playerSettingsHandler;

        private readonly IPrimaryUIServiceInternal _primaryUIService;

        internal PlayerService(PlayerTransformData transformData, PlayerConfig config, HandInteractorContainer interactorContainer, 
            IPlayerPersistentDataHandler playerSettingsHandler, ILocalClientIDProvider playerSyncer, 
            PlayerInputContainer playerInputContainer, IRaycastProvider raycastProvider, IXRManagerWrapper xrManagerWrapper, 
            IPrimaryUIServiceInternal primaryUIService, ISecondaryUIServiceInternal secondaryUIService)
        {
            PlayerTransformData = transformData;
            _config = config;

            _playerInputContainer = playerInputContainer;
            _playerSettingsHandler = playerSettingsHandler;

            if (_config.EnableVR)
            {
                xrManagerWrapper.InitializeLoader(); 

                _playerVR = new PlayerControllerVR(
                    interactorContainer, _playerInputContainer.PlayerVRInputContainer,
                    playerSettingsHandler, new PlayerVRControlConfig(), _config.MovementModeConfig,
                    raycastProvider, xrManagerWrapper, playerSyncer, primaryUIService, secondaryUIService);
            }

            if (_config.Enable2D)
            {
                _player2D = new PlayerController2D(
                    interactorContainer, _playerInputContainer.Player2DInputContainer,
                    playerSettingsHandler, new Player2DControlConfig(), //TODO:
                    raycastProvider, playerSyncer, primaryUIService, secondaryUIService, this);
            }

            _playerSettingsHandler.OnDebugSaveAppearance += HandlePlayerPresentationChanged;
            HandlePlayerPresentationChanged(_playerSettingsHandler.PlayerPresentationConfig); //Do this now to set the initial appearance

            if (_config.EnableVR && !_config.Enable2D)
                PlayerTransformData.IsVRMode = true;
            else if (_config.Enable2D && !_config.EnableVR)
                PlayerTransformData.IsVRMode = false;

            //TODO, figure out what mode to start in? Maybe we need some persistent data to remember the mode in the last scene??
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

            if (_config.Enable2D && _config.EnableVR)
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
            if (!_config.Enable2D || !_config.EnableVR)
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
            }
            catch (System.Exception e)
            {
                Debug.LogError("Error changing player mode: " + e.Message + " - " + e.StackTrace);
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

    public enum LocalPlayerMode
    {
        TwoD, 
        VR
    }
}
