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
        private readonly PlayerStateModule _playerStateModule;
        private readonly PlayerController2D _player2D;
        private readonly PlayerControllerVR _playerVR;
        private bool _enable2D;
        private bool _enableVR;

        private readonly PlayerInputContainer _playerInputContainer;

        private PlayerController _activePlayer => _playerStateModule.PlayerTransformData.IsVRMode? _playerVR : _player2D;

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
                _playerVR = SpawnPlayerVR(interactorContainer, playerSettingsProvider.UserSettings.PlayerVRControlConfig, multiplayerSupport, _playerInputContainer.PlayerVRInputContainer, raycastProvider, xrManagerSettingsWrapper);
            if (enable2D)
                _player2D = SpawnPlayer2D(interactorContainer, playerSettingsProvider.UserSettings.Player2DControlConfig, multiplayerSupport, _playerInputContainer.Player2DInputContainer, raycastProvider);

            //TODO, figure out what mode to start in? Maybe we need some persistent data to remember the mode in the last scene??
            if (_playerStateModule.PlayerTransformData.IsVRMode)
                _playerVR.ActivatePlayer(_playerStateModule.PlayerTransformData);
            else 
                _player2D.ActivatePlayer(_playerStateModule.PlayerTransformData);

            _playerInputContainer.ChangeMode.OnPressed += HandleChangeModePressed;

            HandleAvatarAppearanceChanged(_playerStateModule.AvatarAppearance); //Do this now to set the initial color
        }

        private PlayerController2D SpawnPlayer2D(InteractorContainer interactorContainer, Player2DControlConfig player2DControlConfig, IMultiplayerSupport multiplayerSupport, Player2DInputContainer player2DInputContainer, IRaycastProvider raycastProvider) 
        {
            GameObject player2DPrefab = Resources.Load("2dPlayer") as GameObject;
            GameObject instantiated2DPlayer = GameObject.Instantiate(player2DPrefab, null, false);
            instantiated2DPlayer.SetActive(false);
            PlayerController2D playerController2D = instantiated2DPlayer.GetComponent<PlayerController2D>();
            playerController2D.Initialize(interactorContainer, player2DControlConfig, multiplayerSupport, player2DInputContainer, raycastProvider);
            return playerController2D;
        }

        private PlayerControllerVR SpawnPlayerVR(InteractorContainer interactorContainer, PlayerVRControlConfig playerVRControlConfig, IMultiplayerSupport multiplayerSupport, PlayerVRInputContainer playerVRInputContainer, IRaycastProvider raycastProvider, IXRManagerWrapper xrManagerSettingsWrapper)
        {
            GameObject playerVRPrefab = Resources.Load("vrPlayer") as GameObject;
            GameObject instantiatedVRPlayer = GameObject.Instantiate(playerVRPrefab, null, false);
            instantiatedVRPlayer.SetActive(false);
            PlayerControllerVR playerControllerVR = instantiatedVRPlayer.GetComponent<PlayerControllerVR>();
            playerControllerVR.Initialize(interactorContainer, playerVRControlConfig, multiplayerSupport, playerVRInputContainer, raycastProvider, xrManagerSettingsWrapper);
            return playerControllerVR;
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
        
        public void TearDown() 
        {
            Debug.Log("Tearing down player service - 2d null? " + (_player2D == null) + " - vr null? " + (_playerVR == null));

            //TODO - maybe make these TearDown methods instead?
            if (_player2D != null)
            {
                _player2D.DeactivatePlayer();
                GameObject.DestroyImmediate(_player2D.gameObject);
            }

            if (_playerVR != null)
            {
                _playerVR.DeactivatePlayer();
                GameObject.DestroyImmediate(_playerVR.gameObject);
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
