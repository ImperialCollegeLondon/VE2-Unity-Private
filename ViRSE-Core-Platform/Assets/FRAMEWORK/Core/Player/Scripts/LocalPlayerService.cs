using UnityEngine;
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
                ViRSECoreServiceLocator.Instance.RaycastProvider);
        }
    }

    public class ViRSEPlayerService  
    {
        private readonly PlayerStateModule _playerStateModule;
        private readonly PlayerController2D _player2D;
        private readonly PlayerControllerVR _playerVR;

        private PlayerController _activePlayer => _playerStateModule.PlayerTransformData.IsVRMode? _playerVR : _player2D;

        public ViRSEPlayerService(PlayerTransformData state, PlayerStateConfig config, bool enableVR, bool enable2D, 
            ViRSEPlayerStateModuleContainer virsePlayerStateModuleContainer, IPlayerSettingsProvider playerSettingsProvider, IPlayerAppearanceOverridesProvider playerAppearanceOverridesProvider, 
            IMultiplayerSupport multiplayerSupport, IInputHandler inputHandler, IRaycastProvider raycastProvider)
        {
            _playerStateModule = new(state, config, virsePlayerStateModuleContainer, playerSettingsProvider, playerAppearanceOverridesProvider);
            _playerStateModule.OnAvatarAppearanceChanged += HandleAvatarAppearanceChanged;

            if (enableVR)
                _playerVR = SpawnPlayerVR(playerSettingsProvider.UserSettings.PlayerVRControlConfig, multiplayerSupport, inputHandler, raycastProvider);
            if (enable2D)
                _player2D = SpawnPlayer2D(playerSettingsProvider.UserSettings.Player2DControlConfig, multiplayerSupport, inputHandler, raycastProvider);

            if (_playerStateModule.PlayerTransformData.IsVRMode)
                _playerVR.ActivatePlayer(_playerStateModule.PlayerTransformData);
            else 
                _player2D.ActivatePlayer(_playerStateModule.PlayerTransformData);
        }

        private PlayerController2D SpawnPlayer2D(Player2DControlConfig player2DControlConfig, IMultiplayerSupport multiplayerSupport, IInputHandler inputHandler, IRaycastProvider raycastProvider) 
        {
            GameObject player2DPrefab = Resources.Load("2dPlayer") as GameObject;
            GameObject instantiated2DPlayer = GameObject.Instantiate(player2DPrefab, null, false);
            PlayerController2D playerController2D = instantiated2DPlayer.GetComponent<PlayerController2D>();
            playerController2D.Initialize(player2DControlConfig, multiplayerSupport, inputHandler, raycastProvider);
            return playerController2D;
        }

        private PlayerControllerVR SpawnPlayerVR(PlayerVRControlConfig playerVRControlConfig, IMultiplayerSupport multiplayerSupport, IInputHandler inputHandler, IRaycastProvider raycastProvider)
        {
            GameObject playerVRPrefab = Resources.Load("vrPlayer") as GameObject;
            GameObject instantiatedVRPlayer = GameObject.Instantiate(playerVRPrefab, null, false);
            PlayerControllerVR playerControllerVR = instantiatedVRPlayer.GetComponent<PlayerControllerVR>();
            playerControllerVR.Initialize(playerVRControlConfig, multiplayerSupport, inputHandler, raycastProvider);
            return playerControllerVR;
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
        }
    }

    public enum LocalPlayerMode
    {
        TwoD, 
        VR
    }
}
