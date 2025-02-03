using UnityEngine;
using VE2.Common;
using VE2.Core.Common;
using static VE2.Common.CommonSerializables;

namespace VE2.Core.Player
{
    public static class VE2PlayerServiceFactory
    {
        public static PlayerService Create(PlayerTransformData state, PlayerStateConfig config, bool enableVR, bool enable2D)
        {
            return new PlayerService(state, config, enableVR, enable2D, 
                VE2CoreServiceLocator.Instance.PlayerStateModuleContainer, 
                VE2CoreServiceLocator.Instance.InteractorContainer,
                VE2CoreServiceLocator.Instance.PlayerSettingsProvider, 
                VE2CoreServiceLocator.Instance.PlayerAppearanceOverridesProvider,
                VE2CoreServiceLocator.Instance.MultiplayerSupport, 
                VE2CoreServiceLocator.Instance.InputHandler.PlayerInputContainer,
                new RaycastProvider(),
                VE2CoreServiceLocator.Instance.XRManagerWrapper);
        }
    }

    public class PlayerService  
    {
        public bool VRModeActive => _playerStateModule.PlayerTransformData.IsVRMode;

        private readonly PlayerStateModule _playerStateModule;
        private readonly PlayerController2D _player2D;
        private readonly PlayerControllerVR _playerVR;
        private bool _enable2D;
        private bool _enableVR;

        private readonly PlayerInputContainer _playerInputContainer;

        public PlayerService(PlayerTransformData state, PlayerStateConfig config, bool enableVR, bool enable2D, 
            PlayerStateModuleContainer playerStateModuleContainer, InteractorContainer interactorContainer,
            IPlayerSettingsProvider playerSettingsProvider, IPlayerAppearanceOverridesProvider playerAppearanceOverridesProvider, 
            IMultiplayerSupport multiplayerSupport, PlayerInputContainer playerInputContainer, IRaycastProvider raycastProvider, IXRManagerWrapper xrManagerSettingsWrapper)
        {
            _playerStateModule = new(state, config, playerStateModuleContainer, playerSettingsProvider, playerAppearanceOverridesProvider);
            _playerStateModule.OnAvatarAppearanceChanged += HandleAvatarAppearanceChanged;

            _enable2D = enable2D;
            _enableVR = enableVR;
            _playerInputContainer = playerInputContainer;

            if (enableVR)
            {
                _playerVR = new PlayerControllerVR(
                    interactorContainer, _playerInputContainer.PlayerVRInputContainer, 
                    playerSettingsProvider.UserSettings.PlayerVRControlConfig, 
                    raycastProvider, xrManagerSettingsWrapper, multiplayerSupport);
            }
            if (enable2D)
            {
                _player2D = new PlayerController2D(
                    interactorContainer, _playerInputContainer.Player2DInputContainer, 
                    playerSettingsProvider.UserSettings.Player2DControlConfig, 
                    raycastProvider, multiplayerSupport);
            }

            if (enableVR && !enable2D)
                _playerStateModule.PlayerTransformData.IsVRMode = true;
            else if (enable2D && !enableVR)
                _playerStateModule.PlayerTransformData.IsVRMode = false;

            //TODO, figure out what mode to start in? Maybe we need some persistent data to remember the mode in the last scene??
            if (_playerStateModule.PlayerTransformData.IsVRMode)
                _playerVR.ActivatePlayer(_playerStateModule.PlayerTransformData);
            else 
                _player2D.ActivatePlayer(_playerStateModule.PlayerTransformData);

            _playerInputContainer.ChangeMode.OnPressed += HandleChangeModePressed;

            HandleAvatarAppearanceChanged(_playerStateModule.AvatarAppearance); //Do this now to set the initial color
        }

        private void HandleChangeModePressed() 
        {
            if (!_enable2D || !_enableVR)
                return; //Can't change modes if both aren't enabled!

            try 
            {
                if (_playerStateModule.PlayerTransformData.IsVRMode)
                {
                    _playerVR.DeactivatePlayer();
                    _player2D.ActivatePlayer(_playerStateModule.PlayerTransformData);
                }
                else
                {
                    _player2D.DeactivatePlayer();
                    _playerVR.ActivatePlayer(_playerStateModule.PlayerTransformData);
                }

                _playerStateModule.PlayerTransformData.IsVRMode = !_playerStateModule.PlayerTransformData.IsVRMode;
            }
            catch (System.Exception e)
            {
                Debug.LogError("Error changing player mode: " + e.Message + " - " + e.StackTrace);
            }
        }

        private void HandleAvatarAppearanceChanged(AvatarAppearance appearance)
        {
            //TOOD: Handle head and torso changes 
            _playerVR?.HandleLocalAvatarColorChanged(new Color(
                appearance.PresentationConfig.AvatarRed, 
                appearance.PresentationConfig.AvatarGreen, 
                appearance.PresentationConfig.AvatarBlue) / 255f);
        }   

        public void HandleFixedUpdate()
        {
            if (_playerStateModule.PlayerTransformData.IsVRMode)
                _playerStateModule.PlayerTransformData = _playerVR.PlayerTransformData;
            else 
                _playerStateModule.PlayerTransformData = _player2D.PlayerTransformData;

            _playerStateModule.HandleFixedUpdate();
        }

        public void HandleUpdate() 
        {
            if (_playerStateModule.PlayerTransformData.IsVRMode)
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

            _playerStateModule.TearDown();
            _playerInputContainer.ChangeMode.OnPressed -= HandleChangeModePressed;
        }
    }

    public enum LocalPlayerMode
    {
        TwoD, 
        VR
    }
}
