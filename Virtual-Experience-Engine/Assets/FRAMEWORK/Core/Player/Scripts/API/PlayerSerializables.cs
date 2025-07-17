using System;
using System.IO;
using static VE2.Common.Shared.CommonSerializables;
using System.Collections.Generic;
using VE2.Common.Shared;


#if UNITY_EDITOR
using UnityEngine;
#endif

namespace VE2.Core.Player.API
{
        internal class PlayerSerializables
        {
                [Serializable]
                internal class PlayerVRControlConfig : VE2Serializable
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
                internal class Player2DControlConfig : VE2Serializable
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


                //TODO - this should just be a wrapper, don't like these attributes in here
                //Nope, wrapper doesn't work, API can't see internal
                [Serializable]
                internal class BuiltInPlayerPresentationConfig : VE2Serializable
                {
#if UNITY_EDITOR
                        [BeginIndent, SerializeField, IgnoreParent]
#endif
                        public string PlayerName = "Unknown";

#if UNITY_EDITOR
                        [SerializeField]
#endif
                        public ushort AvatarHeadIndex;

#if UNITY_EDITOR
                        [SerializeField]
#endif
                        public ushort AvatarTorsoIndex;

#if UNITY_EDITOR
                        [SerializeField]
#endif
                        public ushort AvatarColorR = 0;

                        #if UNITY_EDITOR
                        [SerializeField]
#endif
                        public ushort AvatarColorG = 0;

                        #if UNITY_EDITOR
                        [EndIndent, SerializeField]
#endif
                        public ushort AvatarColorB = 0;

#if UNITY_EDITOR
                        public Color AvatarColor
                        {
                                get => new Color(AvatarColorR, AvatarColorG, AvatarColorB) / 255;
                                set
                                {
                                        AvatarColorR = (ushort)(value.r * 255);
                                        AvatarColorG = (ushort)(value.g * 255);
                                        AvatarColorB = (ushort)(value.b * 255);
                                }
                        }       
                        #endif

                        public BuiltInPlayerPresentationConfig() { }

                        public BuiltInPlayerPresentationConfig(byte[] bytes) : base(bytes) { }

#if UNITY_EDITOR
                        public BuiltInPlayerPresentationConfig(string playerName, ushort avatarHeadType, ushort avatarBodyType, Color avatarColor)
                        {
                                PlayerName = playerName;
                                AvatarHeadIndex = avatarHeadType;
                                AvatarTorsoIndex = avatarBodyType;
                                AvatarColor = avatarColor;
                        }

                        public BuiltInPlayerPresentationConfig(BuiltInPlayerPresentationConfig other)
                        {
                                PlayerName = other.PlayerName;
                                AvatarHeadIndex = other.AvatarHeadIndex;
                                AvatarTorsoIndex = other.AvatarTorsoIndex;
                                AvatarColor = other.AvatarColor;
                        }
                        #endif  

                        protected override byte[] ConvertToBytes()
                        {
                                using MemoryStream stream = new();
                                using BinaryWriter writer = new(stream);

                                writer.Write(PlayerName);
                                writer.Write((ushort)AvatarHeadIndex);
                                writer.Write((ushort)AvatarTorsoIndex);
                                writer.Write((ushort)AvatarColorR);
                                writer.Write((ushort)AvatarColorG);
                                writer.Write((ushort)AvatarColorB);

                                return stream.ToArray();
                        }

                        protected override void PopulateFromBytes(byte[] bytes)
                        {
                                using MemoryStream stream = new(bytes);
                                using BinaryReader reader = new(stream);

                                PlayerName = reader.ReadString();
                                AvatarHeadIndex = reader.ReadUInt16();
                                AvatarTorsoIndex = reader.ReadUInt16();
                                AvatarColorR = reader.ReadUInt16();
                                AvatarColorG = reader.ReadUInt16();
                                AvatarColorB = reader.ReadUInt16();
                        }
                        
                        public override bool Equals(object obj)
                        {
                                if (obj is BuiltInPlayerPresentationConfig other)
                                {
                                        return PlayerName == other.PlayerName &&
                                        AvatarHeadIndex == other.AvatarHeadIndex &&
                                        AvatarTorsoIndex == other.AvatarTorsoIndex &&
                                        AvatarColorR == other.AvatarColorR &&
                                        AvatarColorG == other.AvatarColorG &&
                                        AvatarColorB == other.AvatarColorB;
                                }
                                return false;
                        }
                }

                [Serializable]
                internal class PluginAvatarSelections : VE2Serializable
                {
#if UNITY_EDITOR
                        [Title("Head GameObject Selection")]
                        [SerializeField, IgnoreParent, PropertyOrder(0)]
#endif
                        internal AvatarGameObjectSelection HeadGameObjectSelection = new();

#if UNITY_EDITOR
                        [Title("Torso GameObject Selection")]
                        [SerializeField, IgnoreParent, PropertyOrder(2)]
#endif
                        internal AvatarGameObjectSelection TorsoGameObjectSelection = new();

#if UNITY_EDITOR
                        [Title("VR Hand Right GameObject Selection")]
                        [SerializeField, IgnoreParent, PropertyOrder(3)]
#endif
                        internal AvatarGameObjectSelection RightHandVRGameObjectSelection = new();

#if UNITY_EDITOR
                        [Title("VR Hand Left GameObject Selection")]
                        [SerializeField, IgnoreParent, PropertyOrder(4)]
#endif
                        internal AvatarGameObjectSelection LeftHandVRGameObjectSelection = new();

                        public PluginAvatarSelections(
                                AvatarGameObjectSelection headGameObjectSelection,
                                AvatarGameObjectSelection torsoGameObjectSelection,
                                AvatarGameObjectSelection vrHandRightGameObjectSelection,
                                AvatarGameObjectSelection vrHandLeftGameObjectSelection)
                        {
                                HeadGameObjectSelection = headGameObjectSelection;
                                TorsoGameObjectSelection = torsoGameObjectSelection;
                                RightHandVRGameObjectSelection = vrHandRightGameObjectSelection;
                                LeftHandVRGameObjectSelection = vrHandLeftGameObjectSelection;
                        }

                        public PluginAvatarSelections() { }

                        public PluginAvatarSelections(byte[] bytes) : base(bytes) { }

                        protected override byte[] ConvertToBytes()
                        {
                                using MemoryStream stream = new();
                                using BinaryWriter writer = new(stream);

                                byte[] headConfigBytes = HeadGameObjectSelection.Bytes;
                                writer.Write((ushort)headConfigBytes.Length);
                                writer.Write(headConfigBytes);

                                byte[] torsoConfigBytes = TorsoGameObjectSelection.Bytes;
                                writer.Write((ushort)torsoConfigBytes.Length);
                                writer.Write(torsoConfigBytes);

                                byte[] vrHandRightConfigBytes = RightHandVRGameObjectSelection.Bytes;
                                writer.Write((ushort)vrHandRightConfigBytes.Length);
                                writer.Write(vrHandRightConfigBytes);

                                byte[] vrHandLeftConfigBytes = LeftHandVRGameObjectSelection.Bytes;
                                writer.Write((ushort)vrHandLeftConfigBytes.Length);
                                writer.Write(vrHandLeftConfigBytes);

                                return stream.ToArray();
                        }

                        protected override void PopulateFromBytes(byte[] bytes)
                        {
                                using MemoryStream stream = new(bytes);
                                using BinaryReader reader = new(stream);

                                ushort headConfigLength = reader.ReadUInt16();
                                byte[] headConfigBytes = reader.ReadBytes(headConfigLength);
                                HeadGameObjectSelection = new AvatarGameObjectSelection(headConfigBytes);

                                ushort torsoConfigLength = reader.ReadUInt16();
                                byte[] torsoConfigBytes = reader.ReadBytes(torsoConfigLength);
                                TorsoGameObjectSelection = new AvatarGameObjectSelection(torsoConfigBytes);

                                ushort vrHandRightConfigLength = reader.ReadUInt16();
                                byte[] vrHandRightConfigBytes = reader.ReadBytes(vrHandRightConfigLength);
                                RightHandVRGameObjectSelection = new AvatarGameObjectSelection(vrHandRightConfigBytes);

                                ushort vrHandLeftConfigLength = reader.ReadUInt16();
                                byte[] vrHandLeftConfigBytes = reader.ReadBytes(vrHandLeftConfigLength);
                                LeftHandVRGameObjectSelection = new AvatarGameObjectSelection(vrHandLeftConfigBytes);
                        }
                        
                        public override bool Equals(object obj)
                        {
                                if (obj is PluginAvatarSelections other)
                                {
                                        return HeadGameObjectSelection.Equals(other.HeadGameObjectSelection) &&
                                               TorsoGameObjectSelection.Equals(other.TorsoGameObjectSelection) &&
                                               RightHandVRGameObjectSelection.Equals(other.RightHandVRGameObjectSelection) &&
                                               LeftHandVRGameObjectSelection.Equals(other.LeftHandVRGameObjectSelection);
                                }
                                return false;
                        }
                }


                [Serializable]
                internal class AvatarGameObjectSelection : VE2Serializable
                {
#if UNITY_EDITOR
                        [SerializeField]
#endif
                        internal bool BuiltInGameObjectEnabled = true;

#if UNITY_EDITOR
                        [SerializeField]
#endif
                        internal bool CustomGameObjectEnabled = false;

#if UNITY_EDITOR
                        [SerializeField, EnableIf(nameof(CustomGameObjectEnabled), true)]
#endif
                        internal ushort CustomGameObjectIndex = 0;

                        public AvatarGameObjectSelection() { }

                        public AvatarGameObjectSelection(byte[] bytes) : base(bytes) { }

                        protected override byte[] ConvertToBytes()
                        {
                                using MemoryStream stream = new();
                                using BinaryWriter writer = new(stream);

                                writer.Write(BuiltInGameObjectEnabled);
                                writer.Write(CustomGameObjectEnabled);
                                writer.Write(CustomGameObjectIndex);

                                return stream.ToArray();
                        }

                        protected override void PopulateFromBytes(byte[] bytes)
                        {
                                using MemoryStream stream = new(bytes);
                                using BinaryReader reader = new(stream);

                                BuiltInGameObjectEnabled = reader.ReadBoolean();
                                CustomGameObjectEnabled = reader.ReadBoolean();
                                CustomGameObjectIndex = reader.ReadUInt16();
                        }
                }


                [Serializable]
                internal class InstancedAvatarAppearance : VE2Serializable
                {
                        public BuiltInPlayerPresentationConfig BuiltInPresentationConfig { get; set; }
                        public PluginAvatarSelections PlayerGameObjectSelections { get; set; }

                        public InstancedAvatarAppearance() { }

                        public InstancedAvatarAppearance(byte[] bytes) : base(bytes) { }

                        public InstancedAvatarAppearance(BuiltInPlayerPresentationConfig presentationConfig, PluginAvatarSelections playerGameObjectSelections)
                        {
                                BuiltInPresentationConfig = presentationConfig;
                                PlayerGameObjectSelections = playerGameObjectSelections;
                        }

                        protected override byte[] ConvertToBytes()
                        {
                                using MemoryStream stream = new();
                                using BinaryWriter writer = new(stream);

                                byte[] presentationConfigBytes = BuiltInPresentationConfig.Bytes;
                                writer.Write((ushort)presentationConfigBytes.Length);
                                writer.Write(presentationConfigBytes);

                                byte[] playerGameObjectSelectionsBytes = PlayerGameObjectSelections.Bytes;
                                writer.Write((ushort)playerGameObjectSelectionsBytes.Length);
                                writer.Write(playerGameObjectSelectionsBytes);

                                return stream.ToArray();
                        }

                        protected override void PopulateFromBytes(byte[] bytes)
                        {
                                using MemoryStream stream = new(bytes);
                                using BinaryReader reader = new(stream);


                                ushort presentationConfigLength = reader.ReadUInt16();
                                byte[] presentationConfigBytes = reader.ReadBytes(presentationConfigLength);
                                BuiltInPresentationConfig = new BuiltInPlayerPresentationConfig(presentationConfigBytes);


                                ushort playerGameObjectSelectionsLength = reader.ReadUInt16();
                                byte[] playerGameObjectSelectionsBytes = reader.ReadBytes(playerGameObjectSelectionsLength);
                                PlayerGameObjectSelections = new PluginAvatarSelections(playerGameObjectSelectionsBytes);
                        }

                        public override bool Equals(object obj)
                        {
                                if (obj is InstancedAvatarAppearance other)
                                {
                                        return BuiltInPresentationConfig.Equals(other.BuiltInPresentationConfig) &&
                                                PlayerGameObjectSelections.Equals(other.PlayerGameObjectSelections);
                                }
                                return false;
                        }
                }

                internal class PlayerStateWrapper : VE2Serializable //Accessed by the player spawner
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