using System;
using System.IO;
using UnityEngine;
using UnityEngine.UIElements;
using ViRSE.Core.Player;
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
                ViRSECoreServiceLocator.Instance.MultiplayerSupport);
        }
    }

/*
    TODO, we need to listen to input handler so we know when to switch between the two modes 
    We also need functions for changing mode, and for moving the player
    I'm still not totallysold on these things being part of the PlayerService and not the state module 
    the mode itself does live in the state module...
    For the VCs, the interfacs to get state point to the module 
    But for the player, the most up to date state is actually the transforms 
    So makes sense to be getting and setting state on the players directly, the StateModule is basically just a mirror 
    Toggling players off... well, that probably shouldn't be the modules job, that's the playerService 

    For instancing, the syncers are all given a reference to the instance service, and given references to their own dependencies 
    But for the PlayerService, its the PlayerService that creates these "SubServices"? 
    if we were following the patttern, we would be creating the sub-players in the PlayerIntegration, and injecting them with the PlayerService 
    Things are I guess bit different, nothing is dependent on the InstanceService other than the "InstanceSubServices"
    Maybe we don't think of these InstnaceSubServices as part of the InstanceService at all 
    Maybe where we're going wrong is trying to conceptualise the Player as "PlayerService"... is it really a serice? 
    If the integration spawned the players, injecting the service into the players doesn't really make sense 
    The players don't need the service, it's the service that needs the players
*/
    public class ViRSEPlayerService  
    {
        private readonly PlayerStateModule _playerStateModule;
        private readonly PlayerController2D _player2D;
        private readonly PlayerControllerVR _playerVR;

        private PlayerController _activePlayer => _playerStateModule.PlayerTransformData.IsVRMode? _playerVR : _player2D;

        public ViRSEPlayerService(PlayerTransformData state, PlayerStateConfig config, bool enableVR, bool enable2D, 
            ViRSEPlayerStateModuleContainer virsePlayerStateModuleContainer, IPlayerSettingsProvider playerSettingsProvider, IPlayerAppearanceOverridesProvider playerAppearanceOverridesProvider, IMultiplayerSupport multiplayerSupport)
        {
            _playerStateModule = new(state, config, virsePlayerStateModuleContainer, playerSettingsProvider, playerAppearanceOverridesProvider);
            _playerStateModule.OnAvatarAppearanceChanged += HandleAvatarAppearanceChanged;

            if (enableVR)
                _playerVR = SpawnPlayerVR(playerSettingsProvider.UserSettings.PlayerVRControlConfig, multiplayerSupport);
            if (enable2D)
                _player2D = SpawnPlayer2D(playerSettingsProvider.UserSettings.Player2DControlConfig, multiplayerSupport);

            if (_playerStateModule.PlayerTransformData.IsVRMode)
                _playerVR.ActivatePlayer(_playerStateModule.PlayerTransformData);
            else 
                _player2D.ActivatePlayer(_playerStateModule.PlayerTransformData);
        }

        private PlayerController2D SpawnPlayer2D(Player2DControlConfig player2DControlConfig, IMultiplayerSupport multiplayerSupport) 
        {
            GameObject player2DPrefab = Resources.Load("2dPlayer") as GameObject;
            GameObject instantiated2DPlayer = GameObject.Instantiate(player2DPrefab, null, false);
            PlayerController2D playerController2D = instantiated2DPlayer.GetComponent<PlayerController2D>();
            playerController2D.Initialize(player2DControlConfig, multiplayerSupport);
            return playerController2D;
        }

        private PlayerControllerVR SpawnPlayerVR(PlayerVRControlConfig playerVRControlConfig, IMultiplayerSupport multiplayerSupport)
        {
            GameObject playerVRPrefab = Resources.Load("vrPlayer") as GameObject;
            GameObject instantiatedVRPlayer = GameObject.Instantiate(playerVRPrefab, null, false);
            PlayerControllerVR playerControllerVR = instantiatedVRPlayer.GetComponent<PlayerControllerVR>();
            playerControllerVR.Initialize(playerVRControlConfig, multiplayerSupport);
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
