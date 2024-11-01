using System;
using System.IO;

#if UNITY_EDITOR
using UnityEngine;
#endif

namespace ViRSE.Common //TODO - Need to expose to customer 
{
        public class CoreCommonSerializables
        {
                [Serializable]
                public abstract class ViRSESerializable
                {
                        public byte[] Bytes { get => ConvertToBytes(); set => PopulateFromBytes(value); }

                        public ViRSESerializable() { }

                        public ViRSESerializable(byte[] bytes)
                        {
                                PopulateFromBytes(bytes);
                        }

                        protected abstract byte[] ConvertToBytes();

                        protected abstract void PopulateFromBytes(byte[] bytes);
                }


                [Serializable]
                public class UserSettingsPersistable : ViRSESerializable //TODO, probably add audio config 
                {
#if UNITY_EDITOR

                        [Title("2D Control Settings", Order = 2)]
                        [SerializeField, IgnoreParent]
#endif
                        public Player2DControlConfig Player2DControlConfig = new();

#if UNITY_EDITOR
                        [Space(5)]
                        [Title("VR Control Settings")]
                        [SerializeField, IgnoreParent]
#endif
                        public PlayerVRControlConfig PlayerVRControlConfig = new();

#if UNITY_EDITOR
                        [Space(5)]
                        [Title("Avatar Presentation Settings")]
                        [SerializeField, IgnoreParent]
#endif
                        public PlayerPresentationConfig PresentationConfig = new();

                        public UserSettingsPersistable() { }

                        public UserSettingsPersistable(byte[] bytes) : base(bytes) { }

                        protected override byte[] ConvertToBytes()
                        {
                                using MemoryStream stream = new();
                                using BinaryWriter writer = new(stream);

                                byte[] control2DBytes = Player2DControlConfig.Bytes;
                                writer.Write((ushort)control2DBytes.Length);
                                writer.Write(control2DBytes);

                                byte[] controlVRBytes = PlayerVRControlConfig.Bytes;
                                writer.Write((ushort)controlVRBytes.Length);
                                writer.Write(controlVRBytes);

                                byte[] PresentationConfigBytes = PresentationConfig.Bytes;
                                writer.Write((ushort)PresentationConfigBytes.Length);
                                writer.Write(PresentationConfigBytes);

                                return stream.ToArray();
                        }

                        protected override void PopulateFromBytes(byte[] bytes)
                        {
                                using MemoryStream stream = new(bytes);
                                using BinaryReader reader = new(stream);

                                ushort control2DLength = reader.ReadUInt16();
                                byte[] control2DBytes = reader.ReadBytes(control2DLength);
                                Player2DControlConfig = new Player2DControlConfig(control2DBytes);

                                ushort controlVRLength = reader.ReadUInt16();
                                byte[] controlVRBytes = reader.ReadBytes(controlVRLength);
                                PlayerVRControlConfig = new PlayerVRControlConfig(controlVRBytes);

                                ushort PresentationConfigLength = reader.ReadUInt16();
                                byte[] PresentationConfigBytes = reader.ReadBytes(PresentationConfigLength);
                                PresentationConfig = new PlayerPresentationConfig(PresentationConfigBytes);
                        }
                }


                [Serializable]
                public class PlayerVRControlConfig : ViRSESerializable
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
                public class Player2DControlConfig : ViRSESerializable
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
                public class PlayerPresentationConfig : ViRSESerializable //TODO - this should just be a wrapper, don't like these attributes in here
                {
#if UNITY_EDITOR
                        [BeginIndent, SerializeField]
#endif
                        public string PlayerName = "Unknown";

#if UNITY_EDITOR
                        [SerializeField]
#endif
                        public ViRSEAvatarHeadAppearanceType AvatarHeadType;

#if UNITY_EDITOR
                        [SerializeField]
#endif
                        public ViRSEAvatarTorsoAppearanceType AvatarTorsoType;

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

                        public PlayerPresentationConfig(string playerName, ViRSEAvatarHeadAppearanceType avatarHeadType, ViRSEAvatarTorsoAppearanceType avatarBodyType, ushort avatarRed, ushort avatarGreen, ushort avatarBlue)
                        {
                                PlayerName = playerName;
                                AvatarHeadType = avatarHeadType;
                                AvatarTorsoType = avatarBodyType;
                                AvatarRed = avatarRed;
                                AvatarGreen = avatarGreen;
                                AvatarBlue = avatarBlue;
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
                                AvatarHeadType = (ViRSEAvatarHeadAppearanceType)reader.ReadUInt16();
                                AvatarTorsoType = (ViRSEAvatarTorsoAppearanceType)reader.ReadUInt16();
                                AvatarRed = reader.ReadUInt16();
                                AvatarGreen = reader.ReadUInt16();
                                AvatarBlue = reader.ReadUInt16();
                        }
                }

                [Serializable]
                public class ViRSEAvatarAppearance : ViRSESerializable //TODO - needs to contain VR/2D
                {
                        public PlayerPresentationConfig PresentationConfig { get; set; }
                        public AvatarAppearanceOverrideType HeadOverrideType { get; set; }
                        public AvatarAppearanceOverrideType TorsoOverrideType { get; set; }

                        public ViRSEAvatarAppearance() { }

                        public ViRSEAvatarAppearance(byte[] bytes) : base(bytes) { }

                        public ViRSEAvatarAppearance(PlayerPresentationConfig presentationConfig, AvatarAppearanceOverrideType headOverrideType, AvatarAppearanceOverrideType torsoOverrideType)
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
                }

                public enum AvatarAppearanceOverrideType
                {
                        None,
                        OverideOne,
                        OverrideTwo,
                        OverrideThree,
                        OverrideFour,
                        OverrideFive,
                }

                public enum ViRSEAvatarHeadAppearanceType
                {
                        One,
                        Two,
                        Three
                }
                public enum ViRSEAvatarTorsoAppearanceType
                {
                        One,
                        Two,
                        Three
                }

                public class PlayerStateWrapper : ViRSESerializable
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