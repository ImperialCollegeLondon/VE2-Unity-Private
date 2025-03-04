using System;
using System.IO;
using static VE2.Core.Common.CommonSerializables;

#if UNITY_EDITOR
using UnityEngine;
#endif

namespace VE2.Core.Player.API
{
        public class PlayerSerializables
        {
                [Serializable]
                public class PlayerVRControlConfig : VE2Serializable
                {
#if UNITY_EDITOR
                        [BeginIndent, SerializeField, Range(0.1f, 3f)]
#endif
                        public float DragSpeed = 1;

#if UNITY_EDITOR
                        [SerializeField, Range(22.5f, 90)]
#endif
                        public float TurnAmount = 45;

#if UNITY_EDITOR
                        [SerializeField]
#endif
                        public bool ControllerVibration = true;

#if UNITY_EDITOR
                        [SerializeField]
#endif
                        public bool ControllerLabels = true;

#if UNITY_EDITOR
                        [SerializeField, Range(0.5f, 3f)]
#endif
                        public float WristLookPrecision = 1;

                        // VR Comfort settings 
#if UNITY_EDITOR
                        [SerializeField]
#endif
                        public bool DragDarkening = false;

#if UNITY_EDITOR
                        [SerializeField]
#endif
                        public bool TeleportBlink = false;

#if UNITY_EDITOR
                        [EndIndent, SerializeField]
#endif
                        public bool SnapTurnBlink = false;

                        public PlayerVRControlConfig() { }

                        public PlayerVRControlConfig(byte[] bytes) : base(bytes) { }

                        protected override byte[] ConvertToBytes()
                        {
                                using MemoryStream stream = new();
                                using BinaryWriter writer = new(stream);

                                writer.Write(DragSpeed);
                                writer.Write(TurnAmount);
                                writer.Write(ControllerVibration);
                                writer.Write(ControllerLabels);
                                writer.Write(WristLookPrecision);
                                writer.Write(DragDarkening);
                                writer.Write(TeleportBlink);
                                writer.Write(SnapTurnBlink);

                                return stream.ToArray();
                        }

                        protected override void PopulateFromBytes(byte[] bytes)
                        {
                                using MemoryStream stream = new(bytes);
                                using BinaryReader reader = new(stream);

                                DragSpeed = reader.ReadSingle();
                                TurnAmount = reader.ReadSingle();
                                ControllerVibration = reader.ReadBoolean();
                                ControllerLabels = reader.ReadBoolean();
                                WristLookPrecision = reader.ReadSingle();
                                DragDarkening = reader.ReadBoolean();
                                TeleportBlink = reader.ReadBoolean();
                                SnapTurnBlink = reader.ReadBoolean();
                        }
                }

                [Serializable]
                public class Player2DControlConfig : VE2Serializable
                {
#if UNITY_EDITOR
                        [BeginIndent, SerializeField, Range(0.2f, 3f)]
#endif
                        public float MouseSensitivity = 1;

#if UNITY_EDITOR
                        [EndIndent, SerializeField]
#endif
                        public bool CrouchHold = true;

                        //TODO, maybe "TwoDControlPromptClicked" goes here? Actually, maybe not

                        public Player2DControlConfig() { }

                        public Player2DControlConfig(byte[] bytes) : base(bytes) { }

                        protected override byte[] ConvertToBytes()
                        {
                                using MemoryStream stream = new();
                                using BinaryWriter writer = new(stream);

                                writer.Write(MouseSensitivity);
                                writer.Write(CrouchHold);

                                return stream.ToArray();
                        }

                        protected override void PopulateFromBytes(byte[] bytes)
                        {
                                using MemoryStream stream = new(bytes);
                                using BinaryReader reader = new(stream);

                                MouseSensitivity = reader.ReadSingle();
                                CrouchHold = reader.ReadBoolean();
                        }
                }

                [Serializable]
                public class PlayerPresentationConfig : VE2Serializable //TODO - this should just be a wrapper, don't like these attributes in here
                {
#if UNITY_EDITOR
                        [BeginIndent, SerializeField, IgnoreParent]
#endif
                        public string PlayerName = "Unknown";

#if UNITY_EDITOR
                        [SerializeField]
#endif
                        public VE2AvatarHeadAppearanceType AvatarHeadType;

#if UNITY_EDITOR
                        [SerializeField]
#endif
                        public VE2AvatarTorsoAppearanceType AvatarTorsoType;

#if UNITY_EDITOR
                        [SerializeField]
#endif
                        public ushort AvatarRed = 255;

#if UNITY_EDITOR
                        [SerializeField]
#endif
                        public ushort AvatarGreen = 60;

#if UNITY_EDITOR
                        [EndIndent, SerializeField]
#endif
                        public ushort AvatarBlue = 60;

                        public PlayerPresentationConfig() { }

                        public PlayerPresentationConfig(byte[] bytes) : base(bytes) { }

                        public PlayerPresentationConfig(string playerName, VE2AvatarHeadAppearanceType avatarHeadType, VE2AvatarTorsoAppearanceType avatarBodyType, ushort avatarRed, ushort avatarGreen, ushort avatarBlue)
                        {
                                PlayerName = playerName;
                                AvatarHeadType = avatarHeadType;
                                AvatarTorsoType = avatarBodyType;
                                AvatarRed = avatarRed;
                                AvatarGreen = avatarGreen;
                                AvatarBlue = avatarBlue;
                        }

                        public PlayerPresentationConfig(PlayerPresentationConfig other)
                        {
                                PlayerName = other.PlayerName;
                                AvatarHeadType = other.AvatarHeadType;
                                AvatarTorsoType = other.AvatarTorsoType;
                                AvatarRed = other.AvatarRed;
                                AvatarGreen = other.AvatarGreen;
                                AvatarBlue = other.AvatarBlue;
                        }

                        protected override byte[] ConvertToBytes()
                        {
                                using MemoryStream stream = new();
                                using BinaryWriter writer = new(stream);

                                writer.Write(PlayerName);
                                writer.Write((ushort)AvatarHeadType);
                                writer.Write((ushort)AvatarTorsoType);
                                writer.Write(AvatarRed);
                                writer.Write(AvatarGreen);
                                writer.Write(AvatarBlue);

                                return stream.ToArray();
                        }

                        protected override void PopulateFromBytes(byte[] bytes)
                        {
                                using MemoryStream stream = new(bytes);
                                using BinaryReader reader = new(stream);

                                PlayerName = reader.ReadString();
                                AvatarHeadType = (VE2AvatarHeadAppearanceType)reader.ReadUInt16();
                                AvatarTorsoType = (VE2AvatarTorsoAppearanceType)reader.ReadUInt16();
                                AvatarRed = reader.ReadUInt16();
                                AvatarGreen = reader.ReadUInt16();
                                AvatarBlue = reader.ReadUInt16();
                        }
                }

                //TODO: Should live in Player API?
                [Serializable]
                public class OverridableAvatarAppearance : VE2Serializable
                {
                        public PlayerPresentationConfig PresentationConfig { get; set; }
                        public AvatarAppearanceOverrideType HeadOverrideType { get; set; }
                        public AvatarAppearanceOverrideType TorsoOverrideType { get; set; }

                        public OverridableAvatarAppearance() { }

                        public OverridableAvatarAppearance(byte[] bytes) : base(bytes) { }

                        public OverridableAvatarAppearance(PlayerPresentationConfig presentationConfig, AvatarAppearanceOverrideType headOverrideType, AvatarAppearanceOverrideType torsoOverrideType)
                        {
                                PresentationConfig = presentationConfig;
                                HeadOverrideType = headOverrideType;
                                TorsoOverrideType = torsoOverrideType;
                        }

                        protected override byte[] ConvertToBytes()
                        {
                                using MemoryStream stream = new();
                                using BinaryWriter writer = new(stream);

                                byte[] presentationConfigBytes = PresentationConfig.Bytes;
                                writer.Write((ushort)presentationConfigBytes.Length);
                                writer.Write(presentationConfigBytes);

                                writer.Write((ushort)HeadOverrideType);
                                writer.Write((ushort)TorsoOverrideType);

                                return stream.ToArray();
                        }

                        protected override void PopulateFromBytes(byte[] bytes)
                        {
                                using MemoryStream stream = new(bytes);
                                using BinaryReader reader = new(stream);

                                ushort presentationConfigLength = reader.ReadUInt16();
                                byte[] presentationConfigBytes = reader.ReadBytes(presentationConfigLength);
                                PresentationConfig = new PlayerPresentationConfig(presentationConfigBytes);

                                HeadOverrideType = (AvatarAppearanceOverrideType)reader.ReadUInt16();
                                TorsoOverrideType = (AvatarAppearanceOverrideType)reader.ReadUInt16();
                        }

                        public override bool Equals(object obj)
                        {
                                if (obj is OverridableAvatarAppearance other)
                                {
                                        return PresentationConfig.Equals(other.PresentationConfig) &&
                                               HeadOverrideType == other.HeadOverrideType &&
                                               TorsoOverrideType == other.TorsoOverrideType;
                                }
                                return false;
                        }
                }

                //TODO: These should all also probably be in the Player API
                public enum AvatarAppearanceOverrideType
                {
                        None,
                        OverideOne,
                        OverrideTwo,
                        OverrideThree,
                        OverrideFour,
                        OverrideFive,
                }

                public enum VE2AvatarHeadAppearanceType
                {
                        One,
                        Two,
                        Three
                }
                public enum VE2AvatarTorsoAppearanceType
                {
                        One,
                        Two,
                        Three
                }

                public class PlayerStateWrapper : VE2Serializable //Accessed by the player spawner
                {
                        public ushort ID { get; private set; }
                        public byte[] StateBytes { get; private set; }

                        public PlayerStateWrapper(byte[] bytes) : base(bytes) { }

                        public PlayerStateWrapper(ushort id, byte[] state)
                        {
                                ID = id;
                                StateBytes = state;
                        }

                        protected override byte[] ConvertToBytes()
                        {
                                using MemoryStream stream = new MemoryStream();
                                using BinaryWriter writer = new BinaryWriter(stream);

                                writer.Write(ID);
                                writer.Write((ushort)StateBytes.Length);
                                writer.Write(StateBytes);

                                return stream.ToArray();
                        }

                        protected override void PopulateFromBytes(byte[] bytes)
                        {
                                using MemoryStream stream = new(bytes);
                                using BinaryReader reader = new(stream);

                                ID = reader.ReadUInt16();

                                int stateBytesLength = reader.ReadUInt16();
                                StateBytes = reader.ReadBytes(stateBytesLength);
                        }
                }
        }
}