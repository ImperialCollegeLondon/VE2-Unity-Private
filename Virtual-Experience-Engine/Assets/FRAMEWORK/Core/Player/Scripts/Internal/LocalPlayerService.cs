using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using VE2.Common;
using VE2.Core.Common;
using static VE2.Common.CommonSerializables;

namespace VE2.Core.Player
{
    internal static class VE2PlayerServiceFactory
    {
        internal static PlayerService Create(PlayerTransformData state, PlayerConfig config, IPlayerSettingsHandler playerSettingsHandler)
        {
            return new PlayerService(state, config, 
            PlayerLocator.InteractorContainer,
            playerSettingsHandler,
            PlayerLocator.LocalClientIDProviderProvider.LocalClientIDProvider,
            PlayerLocator.InputHandler.PlayerInputContainer,
                new RaycastProvider(),
                new XRManagerWrapper());
        }
    }

    internal class PlayerService : IPlayerService, IPlayerServiceInternal
    {
        #region Interfaces //TODO - this wiring can probably live in the interface?
        public PlayerTransformData PlayerTransformData {get; private set;}
        //public PlayerPresentationConfig PlayerPresentationConfig { get => _playerSettingsHandler.PlayerPresentationConfig; set => _playerSettingsHandler.PlayerPresentationConfig = value; }
        public void MarkPlayerPresentationConfigChanged() 
        {
            _playerSettingsHandler.SavePlayerAppearance();
            OnOverridableAvatarAppearanceChanged?.Invoke(OverridableAvatarAppearance);
        }
        public event Action<OverridableAvatarAppearance> OnOverridableAvatarAppearanceChanged;
        public OverridableAvatarAppearance OverridableAvatarAppearance { 
            get 
            {
                return new OverridableAvatarAppearance(
                    _playerSettingsHandler.PlayerPresentationConfig,
                    _config.HeadOverrideType, 
                    _config.TorsoOverrideType);
            } 
        }

        //This probably SHOULD live in the PlayerSettingsHandler, so it serializes properly 
        //Syncer wants to pick up on changes 
        //So send the entire service 

        public bool RememberPlayerSettings { get => _playerSettingsHandler.RememberPlayerSettings; set => _playerSettingsHandler.RememberPlayerSettings = value; }

        public TransmissionProtocol TransmissionProtocol => _config.RepeatedTransmissionConfig.TransmissionType;
        public float TransmissionFrequency => _config.RepeatedTransmissionConfig.TransmissionFrequency;
        #endregion


        public bool VRModeActive => PlayerTransformData.IsVRMode;

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

        // public GameObject GetHeadOverrideGameObjectForIndex(AvatarAppearanceOverrideType type)
        // {
        //     if (type == AvatarAppearanceOverrideType.None)
        //         return null;
        //     else
        //         return _config.HeadOverrideGOs[(int)type - 1];
        // }

        // public GameObject GetTorsoOverrideGameObjectForIndex(AvatarAppearanceOverrideType type)
        // {
        //     if (type == AvatarAppearanceOverrideType.None)
        //         return null;
        //     else
        //         return _config.TorsoOverrideGOs[(int)type - 1];
        // }



        // PlayerTransformData IPlayerServiceInternal.PlayerTransformData => throw new NotImplementedException();

        // PlayerPresentationConfig IPlayerServiceInternal.PlayerPresentationConfig { get => PlayerPresentationConfig; set => throw new NotImplementedException(); }


        //private readonly PlayerStateModule _playerStateModule;
        private readonly PlayerConfig _config;
        private readonly PlayerController2D _player2D;
        private readonly PlayerControllerVR _playerVR;
        private bool _enable2D;
        private bool _enableVR;

        private readonly PlayerInputContainer _playerInputContainer;
        private readonly IPlayerSettingsHandler _playerSettingsHandler;

        //private readonly IXRManagerWrapper _xrManagerWrapper;


        internal PlayerService(PlayerTransformData transformData, PlayerConfig config,
            InteractorContainer interactorContainer, IPlayerSettingsHandler playerSettingsHandler, 
            ILocalClientIDProvider playerSyncer, PlayerInputContainer playerInputContainer, IRaycastProvider raycastProvider, IXRManagerWrapper xrManagerWrapper)
        {
           // _playerStateModule = new(state, config, playerStateModuleContainer);
            PlayerTransformData = transformData;
            _config = config;

            _playerInputContainer = playerInputContainer;
            _playerSettingsHandler = playerSettingsHandler;
            //_xrManagerWrapper = xrManagerWrapper;


            if (_config.EnableVR)
            {
                xrManagerWrapper.InitializeLoader(); 

                _playerVR = new PlayerControllerVR(
                    interactorContainer, _playerInputContainer.PlayerVRInputContainer,
                    playerSettingsHandler, new PlayerVRControlConfig(), //TODO: 
                    raycastProvider, xrManagerWrapper, playerSyncer);
            }
            if (_config.Enable2D)
            {
                _player2D = new PlayerController2D(
                    interactorContainer, _playerInputContainer.Player2DInputContainer,
                    playerSettingsHandler, new Player2DControlConfig(), //TODO:
                    raycastProvider, playerSyncer);
            }

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

            //HandleAvatarAppearanceChanged(_playerStateModule.AvatarAppearance); //Do this now to set the initial color
        }

        private void HandleChangeModePressed() 
        {
            if (!_enable2D || !_enableVR)
                return; //Can't change modes if both aren't enabled!

            try 
            {
                if (PlayerTransformData.IsVRMode)
                {
                    _playerVR.DeactivatePlayer();
                    _player2D.ActivatePlayer(PlayerTransformData);
                }
                else
                {
                    _player2D.DeactivatePlayer();
                    _playerVR.ActivatePlayer(PlayerTransformData);
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
            _playerVR?.HandleLocalAvatarColorChanged(new Color(
                presentationConfig.AvatarRed,
                presentationConfig.AvatarGreen,
                presentationConfig.AvatarBlue) / 255f);
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

            //_playerStateModule.TearDown();
            _playerInputContainer.ChangeMode.OnPressed -= HandleChangeModePressed;
        }
    }

    public enum LocalPlayerMode
    {
        TwoD, 
        VR
    }
}
