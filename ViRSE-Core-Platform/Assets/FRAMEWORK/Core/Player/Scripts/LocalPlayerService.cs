using UnityEngine;
using UnityEngine.XR.Management;
using ViRSE.Core.Shared;
using static ViRSE.Core.Shared.CoreCommonSerializables;

namespace ViRSE.Core.Player
{
    public static class ViRSEPlayerServiceFactory
    {
        public static ViRSEPlayerService Create(PlayerTransformData state, PlayerStateConfig config, bool enableVR, bool enable2D)
        {
            return new ViRSEPlayerService(state, config, enableVR, enable2D, 
                ViRSECoreServiceLocator.Instance.ViRSEPlayerStateModuleContainer, 
                ViRSECoreServiceLocator.Instance.PlayerSettingsProvider, 
                ViRSECoreServiceLocator.Instance.PlayerAppearanceOverridesProvider,
                ViRSECoreServiceLocator.Instance.MultiplayerSupport, 
                ViRSECoreServiceLocator.Instance.InputHandler,
                ViRSECoreServiceLocator.Instance.RaycastProvider,
                ViRSECoreServiceLocator.Instance.XRManagerWrapper);
        }
    }

    public class ViRSEPlayerService  
    {
        private readonly PlayerStateModule _playerStateModule;
        private readonly PlayerController2D _player2D;
        private readonly PlayerControllerVR _playerVR;
        private bool _enable2D;
        private bool _enableVR;

        private readonly IInputHandler _inputHandler;

        private PlayerController _activePlayer => _playerStateModule.PlayerTransformData.IsVRMode? _playerVR : _player2D;

        public ViRSEPlayerService(PlayerTransformData state, PlayerStateConfig config, bool enableVR, bool enable2D, 
            ViRSEPlayerStateModuleContainer virsePlayerStateModuleContainer, IPlayerSettingsProvider playerSettingsProvider, IPlayerAppearanceOverridesProvider playerAppearanceOverridesProvider, 
            IMultiplayerSupport multiplayerSupport, IInputHandler inputHandler, IRaycastProvider raycastProvider, IXRManagerWrapper xrManagerSettingsWrapper)
        {
            _playerStateModule = new(state, config, virsePlayerStateModuleContainer, playerSettingsProvider, playerAppearanceOverridesProvider);
            _playerStateModule.OnAvatarAppearanceChanged += HandleAvatarAppearanceChanged;

            _enable2D = enable2D;
            _enableVR = enableVR;
            _inputHandler = inputHandler;

            if (enableVR)
                _playerVR = SpawnPlayerVR(playerSettingsProvider.UserSettings.PlayerVRControlConfig, multiplayerSupport, inputHandler, raycastProvider, xrManagerSettingsWrapper);
            if (enable2D)
                _player2D = SpawnPlayer2D(playerSettingsProvider.UserSettings.Player2DControlConfig, multiplayerSupport, inputHandler, raycastProvider);

            //TODO, figure out what mode to start in? Maybe we need some persistent data to remember the mode in the last scene??
            if (_playerStateModule.PlayerTransformData.IsVRMode)
                _playerVR.ActivatePlayer(_playerStateModule.PlayerTransformData);
            else 
                _player2D.ActivatePlayer(_playerStateModule.PlayerTransformData);

            _inputHandler.OnChangeModePressed += HandleChangeModePressed;   
        }

        private PlayerController2D SpawnPlayer2D(Player2DControlConfig player2DControlConfig, IMultiplayerSupport multiplayerSupport, IInputHandler inputHandler, IRaycastProvider raycastProvider) 
        {
            GameObject player2DPrefab = Resources.Load("2dPlayer") as GameObject;
            GameObject instantiated2DPlayer = GameObject.Instantiate(player2DPrefab, null, false);
            instantiated2DPlayer.SetActive(false);
            PlayerController2D playerController2D = instantiated2DPlayer.GetComponent<PlayerController2D>();
            playerController2D.Initialize(player2DControlConfig, multiplayerSupport, inputHandler, raycastProvider);
            return playerController2D;
        }

        private PlayerControllerVR SpawnPlayerVR(PlayerVRControlConfig playerVRControlConfig, IMultiplayerSupport multiplayerSupport, IInputHandler inputHandler, IRaycastProvider raycastProvider, IXRManagerWrapper xrManagerSettingsWrapper)
        {
            GameObject playerVRPrefab = Resources.Load("vrPlayer") as GameObject;
            GameObject instantiatedVRPlayer = GameObject.Instantiate(playerVRPrefab, null, false);
            instantiatedVRPlayer.SetActive(false);
            PlayerControllerVR playerControllerVR = instantiatedVRPlayer.GetComponent<PlayerControllerVR>();
            playerControllerVR.Initialize(playerVRControlConfig, multiplayerSupport, inputHandler, raycastProvider, xrManagerSettingsWrapper);
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

        private void HandleAvatarAppearanceChanged(ViRSEAvatarAppearance appearance)
        {
            //TODO - Change local avatar
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
            //TODO - maybe make these TearDown methods instead?
            if (_player2D != null)
                GameObject.DestroyImmediate(_player2D.gameObject);

            if (_playerVR != null)
                GameObject.DestroyImmediate(_playerVR.gameObject);

            _playerStateModule.TearDown();
            _inputHandler.OnChangeModePressed -= HandleChangeModePressed;
        }
    }

    public enum LocalPlayerMode
    {
        TwoD, 
        VR
    }
}
