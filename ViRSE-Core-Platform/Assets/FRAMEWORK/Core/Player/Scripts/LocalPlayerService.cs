using System;
using UnityEngine;
using UnityEngine.UIElements;
using ViRSE.Core.Player;
using ViRSE.Core.Shared;
using static ViRSE.Core.Shared.CoreCommonSerializables;

namespace ViRSE.Core.Player
{
    public static class ViRSEPlayerFactory
    {
        private const string LOCAL_PLAYER_RIG_PREFAB_PATH = "LocalPlayerRig";

        public static GameObject Create(ViRSESerializable state, BaseStateConfig config, string goName, Transform spawnTransform)
        {
            GameObject localPlayerRigPrefab = Resources.Load(LOCAL_PLAYER_RIG_PREFAB_PATH) as GameObject;
            GameObject instantiatedPlayer = GameObject.Instantiate(localPlayerRigPrefab, spawnTransform.position, spawnTransform.rotation);
            ViRSEPlayer virsePlayer = instantiatedPlayer.GetComponent<ViRSEPlayer>();

            PlayerStateModule playerStateModule = new(state, config, goName, ViRSECoreServiceLocator.Instance.ViRSEPlayerContainer, ViRSECoreServiceLocator.Instance.PlayerSettingsProvider, ViRSECoreServiceLocator.Instance.PlayerAppearanceOverridesProvider, virsePlayer.GetPlayer2DTransform(), Camera.main);
            virsePlayer.Initialize(playerStateModule, ViRSECoreServiceLocator.Instance.PlayerSettingsProvider.UserSettings.Player2DControlConfig, ViRSECoreServiceLocator.Instance.PlayerSettingsProvider.UserSettings.PlayerVRControlConfig);
            return instantiatedPlayer;
        }
    }

    public class PlayerStateModule : BaseStateModule, IPlayerStateModule
    {
        public PlayerState PlayerTransform { get {
            return new(_player2DTransform.position, _player2DTransform.rotation, _player2DCamera.transform.position, _player2DCamera.transform.rotation);
        }}

        public ViRSEAvatarAppearance AvatarAppearance { get {
            if (_appearanceOverridesProvider != null)
                    return new(_playerSettingsProvider.UserSettings.PresentationConfig, _appearanceOverridesProvider.HeadOverrideType, _appearanceOverridesProvider.TorsoOverrideType);
                else
                    return new(_playerSettingsProvider.UserSettings.PresentationConfig, AvatarAppearanceOverrideType.None, AvatarAppearanceOverrideType.None);
            }
        }

        public event Action<ViRSEAvatarAppearance> OnAvatarAppearanceChanged;

        private readonly IPlayerSettingsProvider _playerSettingsProvider;
        private readonly IPlayerAppearanceOverridesProvider _appearanceOverridesProvider;
        private readonly Transform _player2DTransform; //TODO
        private readonly Camera _player2DCamera; //TODO

        public PlayerStateModule(ViRSESerializable state, BaseStateConfig config, string goName, 
                ViRSEPlayerStateModuleContainer playerStateModuleContainer, IPlayerSettingsProvider playerSettingsProvider, IPlayerAppearanceOverridesProvider appearanceOverridesProvider,
                Transform Player2DTransform, Camera Player2DCamera) 
                : base(state, config, goName, playerStateModuleContainer)
        {
            _playerSettingsProvider = playerSettingsProvider;
            _playerSettingsProvider.OnLocalChangeToPlayerSettings += HandleAvatarAppearanceChanged;

            _appearanceOverridesProvider = appearanceOverridesProvider;
            if (_appearanceOverridesProvider != null)
                _appearanceOverridesProvider.OnAppearanceOverridesChanged += HandleAvatarAppearanceChanged;

            _player2DTransform = Player2DTransform;
            _player2DCamera = Player2DCamera;
        }

        private void HandleAvatarAppearanceChanged()
        {
            OnAvatarAppearanceChanged?.Invoke(AvatarAppearance);
        }

        // private void SetPlayerPosition(Vector3 position)
        // {
        //     _activePlayer.transform.position = position;

        //     //How do we move back to the start position? 
        //     //SartPosition lives in the PluginRuntime
        //     //But the ui button that respawns the player lives in PluginRuntime 

        //     //FrameworkRuntime will have to emit an event to PluginService to say "OnPlayerRequestRespawn"
        // }

        // private void SetPlayerRotation(Quaternion rotation)
        // {
        //     _activePlayer.transform.rotation = rotation;
        // }

        public override void TearDown()
        {
            base.TearDown();

            _playerSettingsProvider.OnLocalChangeToPlayerSettings -= HandleAvatarAppearanceChanged;

            if (_appearanceOverridesProvider != null)
                _appearanceOverridesProvider.OnAppearanceOverridesChanged -= HandleAvatarAppearanceChanged;
        }
    }

    //TODO, don't think this should be a mono, should just be a service, that we inject with the 2d and vr players
    public class ViRSEPlayer : MonoBehaviour //, IViRSEPlayerRig //TODO - maybe should be PlayerService? Also, does this actually need to be a monobehaviour?
    {
        [SerializeField, HideInInspector] private LocalPlayerMode _playerMode = LocalPlayerMode.TwoD; //TODO!
        [SerializeField] private PlayerController2D _2dPlayer;
        [SerializeField] private PlayerControllerVR _vrPlayer;
        private PlayerController _activePlayer => _playerMode == LocalPlayerMode.TwoD? _2dPlayer : _vrPlayer;

        private PlayerStateModule _playerStateModule;
        private Player2DControlConfig _player2DControlConfig;
        private PlayerVRControlConfig _playerVRControlConfig;

        //TODO - wire in the state?
        public void Initialize(PlayerStateModule playerStateModule, Player2DControlConfig player2DControlConfig, PlayerVRControlConfig playerVRControlConfig)
        {
            _playerStateModule = playerStateModule;
            _playerStateModule.OnAvatarAppearanceChanged += HandleAvatarAppearanceChanged;

            _player2DControlConfig = player2DControlConfig;
            _playerVRControlConfig = playerVRControlConfig;

            //TODO - instantiate VR and 2D Players, wire in control configs 
            //Sub players should probably inherit from a base, that has an abstract "public ViRSESerializable GetPlayerTransform", the state module can then use this 
        }

        private void HandleAvatarAppearanceChanged(ViRSEAvatarAppearance appearance)
        {
            //TODO - Change local avatar
        }   

        private void Update()
        {
            if (_playerMode == LocalPlayerMode.TwoD)
            {
                //Move vr player to 2d player
            }
        }

        private void FixedUpdate()
        {
            _playerStateModule.HandleFixedUpdate();
        }

        public Transform GetPlayer2DTransform()  //TODO remove
        {
            return _2dPlayer.transform;
        }
    }

    public enum LocalPlayerMode
    {
        TwoD, 
        VR
    }
}
