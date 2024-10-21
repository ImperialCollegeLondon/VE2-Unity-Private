using System;
using System.IO;
using UnityEngine;
using UnityEngine.UIElements;
using ViRSE.Core.Player;
using ViRSE.Core.Shared;
using static ViRSE.Core.Shared.CoreCommonSerializables;

namespace ViRSE.Core.Player
{
    public static class ViRSEPlayerFactory
    {
        private const string PLAYEAR_2D_PATH = "2dPlayer";

        public static ViRSEPlayerService Create(PlayerTransformData state, PlayerStateConfig config)
        {
            //TODO, take in bools for 2d and vr, create VR player

            GameObject player2DPrefab = Resources.Load(PLAYEAR_2D_PATH) as GameObject;
            GameObject instantiated2DPlayer = GameObject.Instantiate(player2DPrefab, null, false);
            PlayerController2D playerController2D = instantiated2DPlayer.GetComponent<PlayerController2D>();
            playerController2D.Initialize(ViRSECoreServiceLocator.Instance.PlayerSettingsProvider.UserSettings.Player2DControlConfig);

            PlayerStateModule playerStateModule = new(state, config, 
                ViRSECoreServiceLocator.Instance.ViRSEPlayerContainer, 
                ViRSECoreServiceLocator.Instance.PlayerSettingsProvider, 
                ViRSECoreServiceLocator.Instance.PlayerAppearanceOverridesProvider);

            return new ViRSEPlayerService(playerStateModule, playerController2D, null);
        }
    }

    public class PlayerStateModule : BaseStateModule, IPlayerStateModule
    {
        public PlayerTransformData PlayerTransformData {
            get => (PlayerTransformData)State; 
            set => State.Bytes = value.Bytes; 
        }

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

        public PlayerStateModule(PlayerTransformData state, BaseStateConfig config, 
                ViRSEPlayerStateModuleContainer playerStateModuleContainer, IPlayerSettingsProvider playerSettingsProvider, IPlayerAppearanceOverridesProvider appearanceOverridesProvider)
                : base(state, config, playerStateModuleContainer)
            
        {
            PlayerTransformData = state;

            _playerSettingsProvider = playerSettingsProvider;
            _playerSettingsProvider.OnLocalChangeToPlayerSettings += HandleAvatarAppearanceChanged;

            _appearanceOverridesProvider = appearanceOverridesProvider;
            if (_appearanceOverridesProvider != null)
                _appearanceOverridesProvider.OnAppearanceOverridesChanged += HandleAvatarAppearanceChanged;
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
    public class ViRSEPlayerService  
    {
        [SerializeField, HideInInspector] private LocalPlayerMode _playerMode = LocalPlayerMode.TwoD; //TODO! Needs to live in the state
        private PlayerController _activePlayer => _playerMode == LocalPlayerMode.TwoD? _player2d : _playerVR;

        private readonly PlayerStateModule _playerStateModule;
        private readonly PlayerController2D _player2d;
        private readonly PlayerControllerVR _playerVR;

        //TODO - wire in the state?
        public ViRSEPlayerService(PlayerStateModule playerStateModule, PlayerController2D player2d, PlayerControllerVR playerVR)
        {
            _playerStateModule = playerStateModule;
            _playerStateModule.OnAvatarAppearanceChanged += HandleAvatarAppearanceChanged;

            _player2d = player2d;
            _playerVR = playerVR;

            if (_playerStateModule.PlayerTransformData.IsVRMode)
            {
                //Enable VR rig
            }
            else 
            {
                _player2d.ActivatePlayer(_playerStateModule.PlayerTransformData);
            }

            //TODO, activate the appripriate player, based on the state 
            //Also, send the state INTO the player in the first place 
        }

        private void HandleAvatarAppearanceChanged(ViRSEAvatarAppearance appearance)
        {
            //TODO - Change local avatar
        }   

        public void HandleFixedUpdate()
        {
            if (_playerMode == LocalPlayerMode.TwoD)
            {
                _playerStateModule.PlayerTransformData = _player2d.PlayerTransformData;
                //Right, I can't completely overwrite the object, because it needs to persist 
                //I don't really want to have to convert to/from bytes at this stage 
                //Don't want to write to bytes if I'm not even going to transmit 
                //Thing is though, the syncer will be looking for changes in the bytes, so I need to write to bytes anyway
                //Ok, well, unless we put an equals method in the state interface, which probably makes sense, we only
                //want to write to bytes when we actually transmit?

                //Could just put this transform thing in a "CoreCommonNonSerializables" object?
            }
            _playerStateModule.HandleFixedUpdate();
        }
        
        public void TearDown() 
        {
            //TODO - maybe make these TearDown methods instead?
            if (_player2d != null)
                GameObject.DestroyImmediate(_player2d.gameObject);

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

    //TODO - FIND A PROPER HOME FOR THIS
    //NOTE - The AvatarAppearanceWrapper can just take a byte array for the appearance, doesn't need the actual appearance object 

    //TODO, we know the player will always be rotated to be level with the floor 
    //So that means we can actually just transmit a single float for the root rotation angle
    [Serializable]
    public class PlayerTransformData : ViRSESerializable
    {
        public bool IsVRMode { get; private set; }

        public Vector3 RootPosition;
        public Quaternion RootRotation;
        public Vector3 HeadLocalPosition { get; private set; }
        public Quaternion HeadLocalRotation { get; private set; }
        public Vector3 Hand2DLocalPosition { get; private set; }
        public Quaternion Hand2DLocalRotation { get; private set; }
        public Vector3 HandVRLeftLocalPosition { get; private set; }
        public Quaternion HandVRLeftLocalRotation { get; private set; }
        public Vector3 HandVRRightLocalPosition { get; private set; }
        public Quaternion HandVRRightLocalRotation { get; private set; }

        public PlayerTransformData(byte[] bytes) : base(bytes) { }

        public PlayerTransformData() : base() { }

        public PlayerTransformData(bool IsVRMode, Vector3 rootPosition, Quaternion rootRotation, Vector3 headPosition, Quaternion headRotation, Vector3 hand2DPosition, Quaternion hand2DRotation)
        {
            this.IsVRMode = IsVRMode;
            if (IsVRMode)
                Debug.LogError("PlayerTransformData created with VR mode, but only given a single hand transform - perhaps you called the wrong constructor? " + Environment.StackTrace);

            RootPosition = rootPosition;
            RootRotation = rootRotation;
            HeadLocalPosition = headPosition;
            HeadLocalRotation = headRotation;
            Hand2DLocalPosition = hand2DPosition;
            Hand2DLocalRotation = hand2DRotation;
        }

        public PlayerTransformData(bool IsVRMode, Vector3 rootPosition, Quaternion rootRotation, Vector3 headPosition, Quaternion headRotation, Vector3 handVRLeftPosition, Quaternion handVRLeftRotation, Vector3 handVRRightPosition, Quaternion handVRRightRotation)
        {
            this.IsVRMode = IsVRMode;
            if (!IsVRMode)
                Debug.LogError("PlayerTransformData created with 2D mode, but only given two hand transforms - perhaps you called the wrong constructor? " + Environment.StackTrace);

            RootPosition = rootPosition;
            RootRotation = rootRotation;
            HeadLocalPosition = headPosition;
            HeadLocalRotation = headRotation;
            HandVRLeftLocalPosition = handVRLeftPosition;
            HandVRLeftLocalRotation = handVRLeftRotation;
            HandVRRightLocalPosition = handVRRightPosition;
            HandVRRightLocalRotation = handVRRightRotation;
        }


        protected override byte[] ConvertToBytes()
        {
            using MemoryStream stream = new();
            using BinaryWriter writer = new(stream);

            writer.Write(IsVRMode);

            writer.Write(RootPosition.x);
            writer.Write(RootPosition.y);
            writer.Write(RootPosition.z);

            writer.Write(RootRotation.x);
            writer.Write(RootRotation.y);
            writer.Write(RootRotation.z);
            writer.Write(RootRotation.w);

            writer.Write(HeadLocalPosition.x);
            writer.Write(HeadLocalPosition.y);
            writer.Write(HeadLocalPosition.z);

            writer.Write(HeadLocalRotation.x);
            writer.Write(HeadLocalRotation.y);
            writer.Write(HeadLocalRotation.z);
            writer.Write(HeadLocalRotation.w);

            if (!IsVRMode)
            {
                writer.Write(Hand2DLocalPosition.x);
                writer.Write(Hand2DLocalPosition.y);
                writer.Write(Hand2DLocalPosition.z);

                writer.Write(Hand2DLocalRotation.x);
                writer.Write(Hand2DLocalRotation.y);
                writer.Write(Hand2DLocalRotation.z);
                writer.Write(Hand2DLocalRotation.w);
            }
            else 
            {
                writer.Write(HandVRLeftLocalPosition.x);
                writer.Write(HandVRLeftLocalPosition.y);
                writer.Write(HandVRLeftLocalPosition.z);

                writer.Write(HandVRLeftLocalRotation.x);
                writer.Write(HandVRLeftLocalRotation.y);
                writer.Write(HandVRLeftLocalRotation.z);
                writer.Write(HandVRLeftLocalRotation.w);

                writer.Write(HandVRRightLocalPosition.x);
                writer.Write(HandVRRightLocalPosition.y);
                writer.Write(HandVRRightLocalPosition.z);

                writer.Write(HandVRRightLocalRotation.x);
                writer.Write(HandVRRightLocalRotation.y);
                writer.Write(HandVRRightLocalRotation.z);
                writer.Write(HandVRRightLocalRotation.w);
            }

            return stream.ToArray();
        }

        protected override void PopulateFromBytes(byte[] data)
        {
            using MemoryStream stream = new(data);
            using BinaryReader reader = new(stream);

            IsVRMode = reader.ReadBoolean();
            RootPosition = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
            RootRotation = new Quaternion(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
            HeadLocalPosition = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
            HeadLocalRotation = new Quaternion(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());

            if (!IsVRMode)
            {
                Hand2DLocalPosition = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
                Hand2DLocalRotation = new Quaternion(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
            }
            else 
            {
                HandVRLeftLocalPosition = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
                HandVRLeftLocalRotation = new Quaternion(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
                HandVRRightLocalPosition = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
                HandVRRightLocalRotation = new Quaternion(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
            }
        }
    }
}
