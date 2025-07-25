
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using static VE2.Core.Player.API.PlayerSerializables;
using UnityEngine;
using static VE2.NonCore.Platform.API.PlatformPublicSerializables;
using ClientInfoBase = VE2.NonCore.Platform.API.PlatformPublicSerializables.ClientInfoBase;
using static VE2.Common.Shared.CommonSerializables;
using static VE2.NonCore.Instancing.API.InstancePublicSerializables;

namespace VE2.NonCore.Instancing.Internal
{
    internal class InstanceSyncSerializables
    {
        public static readonly int InstanceNetcodeVersion = 9;

        public enum InstanceNetworkingMessageCodes
        {
            NetcodeVersionConfirmation,
            ServerRegistrationRequest,
            ServerRegistrationConfirmation,
            InstanceInfo,
            WorldstateSyncableBundle,
            PlayerState,
            UpdateAvatarPresentation,
            AdminUpdateNotice,
            PingMessage,
            InstantMessage
        }

        public class PingMessage : VE2Serializable
        {
            public int PingId { get; private set; }
            public ushort ClientId { get; private set; }

            public PingMessage(byte[] bytes) : base(bytes) { }

            public PingMessage(int pingId, ushort clientId)
            {
                this.PingId = pingId;
                this.ClientId = clientId;
            }

            protected override byte[] ConvertToBytes()
            {
                using MemoryStream stream = new();
                using BinaryWriter writer = new(stream);

                writer.Write(PingId);
                writer.Write(ClientId);

                return stream.ToArray();
            }

            protected override void PopulateFromBytes(byte[] bytes)
            {
                using MemoryStream stream = new(bytes);
                using BinaryReader reader = new(stream);

                PingId = reader.ReadInt32();
                ClientId = reader.ReadUInt16();
            }
        }

        public class InstantMessage : VE2Serializable
        {
            public string Id { get; private set; }
            private MemoryStream _serializedMessageObject = new();
            public MemoryStream SerializedMessageObject
            {
                get
                {
                    _serializedMessageObject.Position = 0;
                    return _serializedMessageObject;
                }
                set => _serializedMessageObject = value;
            }

            public InstantMessage(byte[] bytes) : base(bytes) { }

            public InstantMessage(string id, object message)
            {
                Id =  id;

                try
                {
                    BinaryFormatter binaryFormatter = new();
                    SerializedMessageObject.SetLength(0);
                    binaryFormatter.Serialize(SerializedMessageObject, message);
                }
                catch (Exception e)
                {
                    Debug.Log($"Error encountered when trying to serialize MessageObject with ID {id} \n{e.Message}\n{e.StackTrace}");
                    return;
                }
            }

            protected override byte[] ConvertToBytes()
            {
                using MemoryStream stream = new();
                using BinaryWriter writer = new(stream);

                writer.Write(Id);

                writer.Write((int)SerializedMessageObject.Length);

                if (SerializedMessageObject.Length > 0)
                    SerializedMessageObject.CopyTo(stream);

                return stream.ToArray();
            }

            protected override void PopulateFromBytes(byte[] data)
            {
                using MemoryStream stream = new(data);
                using BinaryReader reader = new(stream);

                Id = reader.ReadString();

                int bytesLength = reader.ReadInt32();

                SerializedMessageObject.SetLength(0);
                byte[] messageObjectBytes = reader.ReadBytes(bytesLength);
                if (bytesLength > 0)
                    SerializedMessageObject.Write(messageObjectBytes, 0, messageObjectBytes.Length);
            }
        }

        //So what actually is this registration request?
        //Needs to have the avatar presentation details
        //Also needs the instance code 

        public class ServerRegistrationRequest : VE2Serializable
        {
            public InstanceCode InstanceCode { get; private set; }
            public ushort IDToRestore { get; private set; }
            public AvatarAppearanceWrapper AvatarAppearanceWrapper { get; private set; } 

            public ServerRegistrationRequest(byte[] bytes) : base(bytes) { }

            public ServerRegistrationRequest(InstanceCode instanceCode, ushort idToRestore, AvatarAppearanceWrapper avatarAppearanceWrapper)
            {
                InstanceCode = instanceCode;
                IDToRestore = idToRestore;
                AvatarAppearanceWrapper = avatarAppearanceWrapper;
            }

            protected override byte[] ConvertToBytes()
            {
                using MemoryStream stream = new();
                using BinaryWriter writer = new(stream);

                writer.Write(InstanceCode.ToString());
                writer.Write(IDToRestore);

                byte[] avatarAppearanceBytes = AvatarAppearanceWrapper.Bytes;
                writer.Write((ushort)avatarAppearanceBytes.Length);
                writer.Write(avatarAppearanceBytes);

                return stream.ToArray();
            }

            protected override void PopulateFromBytes(byte[] bytes)
            {
                using MemoryStream stream = new(bytes);
                using BinaryReader reader = new(stream);

                InstanceCode = new(reader.ReadString());
                IDToRestore = reader.ReadUInt16();

                ushort avatarAppearanceBytesLength = reader.ReadUInt16();
                AvatarAppearanceWrapper = new AvatarAppearanceWrapper(reader.ReadBytes(avatarAppearanceBytesLength));
            }
        }

        public class ServerRegistrationConfirmation : VE2Serializable
        {
            public ushort LocalClientID { get; private set; }
            public InstancedInstanceInfo InstanceInfo { get; private set; }

            public ServerRegistrationConfirmation(byte[] bytes) : base(bytes) { }

            public ServerRegistrationConfirmation(ushort localClientID, InstancedInstanceInfo instanceInfo)
            {
                LocalClientID = localClientID;
                InstanceInfo = instanceInfo;
            }

            protected override byte[] ConvertToBytes()
            {
                using MemoryStream stream = new();
                using BinaryWriter writer = new(stream);

                writer.Write(LocalClientID);

                byte[] bytes = InstanceInfo.Bytes;
                writer.Write((ushort)bytes.Length);
                writer.Write(bytes);

                return stream.ToArray();
            }

            protected override void PopulateFromBytes(byte[] bytes)
            {
                using MemoryStream stream = new(bytes);
                using BinaryReader reader = new(stream);

                LocalClientID = reader.ReadUInt16();
                ushort instanceInfoLength = reader.ReadUInt16();
                InstanceInfo = new InstancedInstanceInfo(reader.ReadBytes(instanceInfoLength));
            }
        }

        public class AdminUpdateNotice : VE2Serializable
        {
            public bool IsAdmin { get; private set; }

            public AdminUpdateNotice(byte[] bytes) : base(bytes) { }

            public AdminUpdateNotice(bool isAdmin)
            {
                IsAdmin = isAdmin;
            }

            protected override byte[] ConvertToBytes()
            {
                using MemoryStream stream = new();
                using BinaryWriter writer = new(stream);

                writer.Write(IsAdmin);
                return stream.ToArray();
            }

            protected override void PopulateFromBytes(byte[] bytes)
            {
                using MemoryStream stream = new(bytes);
                using BinaryReader reader = new(stream);

                IsAdmin = reader.ReadBoolean();
            }
        }

        public class WorldStateSnapshot : VE2Serializable
        {
            public string InstanceCode { get; private set; }
            public WorldStateBundle WorldStateBundle { get; private set; }

            public WorldStateSnapshot(byte[] bytes) : base(bytes) { }

            public WorldStateSnapshot(string instanceCode, WorldStateBundle worldStateBundle)
            {
                InstanceCode = instanceCode;
                WorldStateBundle = worldStateBundle;
            }

            protected override byte[] ConvertToBytes()
            {
                using MemoryStream stream = new MemoryStream();
                using BinaryWriter writer = new BinaryWriter(stream);

                writer.Write(InstanceCode);

                byte[] WorldStateBundleBytes = WorldStateBundle.Bytes;
                writer.Write(WorldStateBundleBytes.Length);
                writer.Write(WorldStateBundleBytes);

                return stream.ToArray();
            }

            protected override void PopulateFromBytes(byte[] bytes)
            {
                using MemoryStream stream = new(bytes);
                using BinaryReader reader = new(stream);

                InstanceCode = reader.ReadString();

                int worldStateBundleBytesLength = reader.ReadInt32();
                WorldStateBundle = new(reader.ReadBytes(worldStateBundleBytesLength));
            }
        }

        public class WorldStateBundle : VE2Serializable
        {
            public List<WorldStateWrapper> WorldStateWrappers { get; private set; } = new();

            public WorldStateBundle(byte[] bytes) : base(bytes) { }

            public WorldStateBundle(List<WorldStateWrapper> serializedSyncableStates)
            {
                WorldStateWrappers = serializedSyncableStates;
            }

            protected override byte[] ConvertToBytes()
            {
                using MemoryStream stream = new MemoryStream();
                using BinaryWriter writer = new BinaryWriter(stream);

                writer.Write((ushort)WorldStateWrappers.Count);
                foreach (WorldStateWrapper worldStateWrapper in WorldStateWrappers)
                {
                    byte[] worldStateWrapperBytes = worldStateWrapper.Bytes;
                    writer.Write((ushort)worldStateWrapperBytes.Length);
                    writer.Write(worldStateWrapperBytes);
                }

                return stream.ToArray();
            }

            protected override void PopulateFromBytes(byte[] bytes)
            {
                using MemoryStream stream = new(bytes);
                using BinaryReader reader = new(stream);

                ushort worldStateWrappersCount = reader.ReadUInt16();
                WorldStateWrappers = new List<WorldStateWrapper>();

                for (int i = 0; i < worldStateWrappersCount; i++)
                {
                    int stateWrapperBytesLength = reader.ReadUInt16();
                    byte[] stateWrapperBytes = reader.ReadBytes(stateWrapperBytesLength);
                    WorldStateWrappers.Add(new WorldStateWrapper(stateWrapperBytes));
                }
            }
        }

        public class WorldStateWrapper : VE2Serializable
        {
            public string ID { get; private set; }
            public byte[] StateBytes { get; private set; }

            public WorldStateWrapper(byte[] bytes) : base(bytes) { }

            public WorldStateWrapper(string id, byte[] state)
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

                ID = reader.ReadString();

                int stateBytesLength = reader.ReadUInt16();
                StateBytes = reader.ReadBytes(stateBytesLength);
            }
        }
    }
}
