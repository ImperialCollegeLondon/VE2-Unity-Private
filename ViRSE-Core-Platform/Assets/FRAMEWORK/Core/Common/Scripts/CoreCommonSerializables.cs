using System;
using System.IO;

#if UNITY_EDITOR
using UnityEngine;
#endif

namespace ViRSE.Core.Shared
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


    //VR settings=========================================================================================================

    [Serializable]
        public class PlayerVRControlConfig : ViRSESerializable
        {
#if UNITY_EDITOR
            [SerializeField, Range(0.1f, 3f)]
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
            [SerializeField]
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

        //2D settings=========================================================================================================

        [Serializable]
        public class Player2DControlConfig : ViRSESerializable
        {
#if UNITY_EDITOR
            [SerializeField, Range(0.2f, 3f)]
#endif
            public float MouseSensitivity = 1;

#if UNITY_EDITOR
            [SerializeField]
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
        public class PlayerPresentationConfig : ViRSESerializable
        {
            public string PlayerName = "Unknown";
            public string AvatarHeadType = "ViRSE-0";
            public string AvatarBodyType = "ViRSE-0";
            public ushort AvatarRed = 255;
            public ushort AvatarGreen = 60;
            public ushort AvatarBlue = 60;

            public PlayerPresentationConfig() { }

            public PlayerPresentationConfig(byte[] bytes) : base(bytes) { }

            public PlayerPresentationConfig(string playerName, string avatarHeadType, string avatarBodyType, ushort avatarRed, ushort avatarGreen, ushort avatarBlue)
            {
                PlayerName = playerName;
                AvatarHeadType = avatarHeadType;
                AvatarBodyType = avatarBodyType;
                AvatarRed = avatarRed;
                AvatarGreen = avatarGreen;
                AvatarBlue = avatarBlue;
            }

            protected override byte[] ConvertToBytes()
            {
                using MemoryStream stream = new();
                using BinaryWriter writer = new(stream);

                writer.Write(PlayerName);
                writer.Write(AvatarHeadType);
                writer.Write(AvatarBodyType);
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
                AvatarHeadType = reader.ReadString();
                AvatarBodyType = reader.ReadString();
                AvatarRed = reader.ReadUInt16();
                AvatarGreen = reader.ReadUInt16();
                AvatarBlue = reader.ReadUInt16();
            }
        }

        [Serializable]
        public class PlayerPresentationOverrides : ViRSESerializable
        {
#if UNITY_EDITOR
            [HideInInspector]
#endif
            public bool UsingViRSEAvatar = true; //TODO remove? 

            public string AvatarHeadTypeOverride = string.Empty;

            public string AvatarBodyTypeOverride = string.Empty;

            public bool AvatarTransparancy = false;

            public PlayerPresentationOverrides() { }

            public PlayerPresentationOverrides(byte[] bytes) : base(bytes) { }

            public PlayerPresentationOverrides(bool usingViRSEAvatar, string avatarHeadTypeOverride, string avatarBodyTypeOverride, bool avatarTransparancy)
            {
                UsingViRSEAvatar = usingViRSEAvatar;
                AvatarHeadTypeOverride = avatarHeadTypeOverride;
                AvatarBodyTypeOverride = avatarBodyTypeOverride;
                AvatarTransparancy = avatarTransparancy;
            }

            protected override byte[] ConvertToBytes()
            {
                using MemoryStream stream = new();
                using BinaryWriter writer = new(stream);

                writer.Write(UsingViRSEAvatar);
                writer.Write(AvatarHeadTypeOverride);
                writer.Write(AvatarBodyTypeOverride);
                writer.Write(AvatarTransparancy);

                return stream.ToArray();
            }

            protected override void PopulateFromBytes(byte[] bytes)
            {
                using MemoryStream stream = new(bytes);
                using BinaryReader reader = new(stream);

                UsingViRSEAvatar = reader.ReadBoolean();
                AvatarHeadTypeOverride = reader.ReadString();
                AvatarBodyTypeOverride = reader.ReadString();
                AvatarTransparancy = reader.ReadBoolean();
            }
        }
    }
}