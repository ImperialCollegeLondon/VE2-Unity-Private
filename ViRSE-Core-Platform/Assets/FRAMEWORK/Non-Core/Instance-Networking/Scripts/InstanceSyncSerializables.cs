using System.Collections;
using System.Collections.Generic;
using System.IO;
using ViRSE.Core.Shared;
using static NonCoreCommonSerializables;
using static ViRSE.Core.Shared.CoreCommonSerializables;

#if UNITY_EDITOR
using UnityEngine;
#endif

public class InstanceSyncSerializables
{
    public static readonly int InstanceNetcodeVersion = 5;

    public enum InstanceNetworkingMessageCodes
    {
        NetcodeVersionConfirmation,
        ServerRegistrationRequest,
        ServerRegistrationConfirmation,
        InstanceInfo,
        WorldstateSyncableBundle,
        PlayerState,
        UpdateAvatarPresentation,
    }

    //So what actually is this registration request?
    //Needs to have the avatar presentation details
    //Also needs the instance code 

    public class ServerRegistrationRequest : ViRSESerializable
    {
        public string InstanceCode { get; private set; }
        public InstancedPlayerPresentation AvatarDetails { get; private set; }

        public ServerRegistrationRequest(byte[] bytes) : base(bytes) { }

        public ServerRegistrationRequest(InstancedPlayerPresentation avatarDetails, string instanceCode)
        {
            AvatarDetails = avatarDetails;
            InstanceCode = instanceCode;
        }

        protected override byte[] ConvertToBytes()
        {
            using MemoryStream stream = new();
            using BinaryWriter writer = new(stream);

            writer.Write(InstanceCode);
            writer.Write((ushort)AvatarDetails.Bytes.Length);
            writer.Write(AvatarDetails.Bytes);

            return stream.ToArray();
        }

        protected override void PopulateFromBytes(byte[] bytes)
        {
            using MemoryStream stream = new(bytes);
            using BinaryReader reader = new(stream);

            InstanceCode = reader.ReadString();
            ushort avatarDetailsLength = reader.ReadUInt16();
            AvatarDetails = new InstancedPlayerPresentation(reader.ReadBytes(avatarDetailsLength));
        }
    }

    public class ServerRegistrationConfirmation : ViRSESerializable
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
            writer.Write((ushort)InstanceInfo.Bytes.Length);
            writer.Write(InstanceInfo.Bytes);

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

    public class InstancedInstanceInfo : InstanceInfoBase
    {
        public ushort HostID;
        public bool InstanceMuted; //TODO should this live here?
        public Dictionary<ushort, InstancedClientInfo> ClientInfos;

        public InstancedInstanceInfo()
        {
            ClientInfos = new Dictionary<ushort, InstancedClientInfo>();
        }

        public InstancedInstanceInfo(byte[] bytes) : base(bytes) { }

        public InstancedInstanceInfo(string worldName, string instanceSuffix, ushort hostID, bool instanceMuted, Dictionary<ushort, InstancedClientInfo> clientInfos) : base(worldName, instanceSuffix)
        {
            HostID = hostID;
            InstanceMuted = instanceMuted;
            ClientInfos = clientInfos;
        }

        protected override byte[] ConvertToBytes()
        {
            using MemoryStream stream = new();
            using BinaryWriter writer = new(stream);

            byte[] baseBytes = base.ConvertToBytes();
            writer.Write((ushort)baseBytes.Length);
            writer.Write(baseBytes);

            writer.Write(HostID);
            writer.Write(InstanceMuted);

            writer.Write((ushort)ClientInfos.Count);
            foreach (var kvp in ClientInfos)
            {
                writer.Write(kvp.Key);
                writer.Write((ushort)kvp.Value.Bytes.Length);
                writer.Write(kvp.Value.Bytes);
            }

            return stream.ToArray();
        }

        protected override void PopulateFromBytes(byte[] bytes)
        {
            using MemoryStream stream = new(bytes);
            using BinaryReader reader = new(stream);

            ushort baseBytesLength = reader.ReadUInt16();
            byte[] baseData = reader.ReadBytes(baseBytesLength);
            base.PopulateFromBytes(baseData);

            HostID = reader.ReadUInt16();
            InstanceMuted = reader.ReadBoolean();

            ushort clientCount = reader.ReadUInt16();
            ClientInfos = new Dictionary<ushort, InstancedClientInfo>(clientCount);
            for (int i = 0; i < clientCount; i++)
            {
                ushort key = reader.ReadUInt16();
                ushort length = reader.ReadUInt16();
                InstancedClientInfo value = new InstancedClientInfo(reader.ReadBytes(length));
                ClientInfos.Add(key, value);
            }
        }
    }

    public class InstancedClientInfo : ClientInfoBase
    {
        public InstancedPlayerPresentation InstancedAvatarAppearance;

        public InstancedClientInfo() { }

        public InstancedClientInfo(byte[] bytes) : base(bytes) { }

        public InstancedClientInfo(ushort clientID, bool isAdmin, InstancedPlayerPresentation instancedAvatarAppearance) : base(clientID, isAdmin, "unknown") //TODO, machine name should maybe be platform-specific?
        {
            InstancedAvatarAppearance = instancedAvatarAppearance;
        }

        protected override byte[] ConvertToBytes()
        {
            using MemoryStream stream = new();
            using BinaryWriter writer = new(stream);

            byte[] baseBytes = base.ConvertToBytes();
            writer.Write((ushort)baseBytes.Length);
            writer.Write(baseBytes);

            writer.Write((ushort)InstancedAvatarAppearance.Bytes.Length);
            writer.Write(InstancedAvatarAppearance.Bytes);

            return stream.ToArray();
        }

        protected override void PopulateFromBytes(byte[] bytes)
        {
            using MemoryStream stream = new(bytes);
            using BinaryReader reader = new(stream);

            ushort baseBytesLength = reader.ReadUInt16();
            byte[] baseData = reader.ReadBytes(baseBytesLength);
            base.PopulateFromBytes(baseData);

            ushort avatarAppearanceLength = reader.ReadUInt16();
            InstancedAvatarAppearance = new InstancedPlayerPresentation(reader.ReadBytes(avatarAppearanceLength));
        }
    }

    public class InstancedPlayerPresentation : ViRSESerializable
    {
        public PlayerPresentationConfig PlayerPresentationConfig;
        public PlayerPresentationOverrides PlayerPresentationOverrides;

        public InstancedPlayerPresentation() { }

        public InstancedPlayerPresentation(byte[] bytes) : base(bytes) { }

        public InstancedPlayerPresentation(PlayerPresentationConfig instancedAvatarAppearance, PlayerPresentationOverrides playerPresentationOverrides)
        {
            PlayerPresentationConfig = instancedAvatarAppearance;
            PlayerPresentationOverrides = playerPresentationOverrides;
        }

        protected override byte[] ConvertToBytes()
        {
            using MemoryStream stream = new();
            using BinaryWriter writer = new(stream);

            byte[] presentationBytes = PlayerPresentationConfig.Bytes;
            writer.Write((ushort)presentationBytes.Length);
            writer.Write(presentationBytes);

            byte[] overrideBytes = PlayerPresentationOverrides.Bytes;
            writer.Write((ushort)overrideBytes.Length);
            writer.Write(overrideBytes);

            return stream.ToArray();
        }

        protected override void PopulateFromBytes(byte[] bytes)
        {
            using MemoryStream stream = new(bytes);
            using BinaryReader reader = new(stream);

            ushort presentationBytesLength = reader.ReadUInt16();
            byte[] presentationBytes = reader.ReadBytes(presentationBytesLength);
            PlayerPresentationConfig = new(presentationBytes);

            ushort overridesBytesLength = reader.ReadUInt16();
            byte[] overrideBytes = reader.ReadBytes(overridesBytesLength);
            PlayerPresentationOverrides = new(overrideBytes);
        }
    }


    public class WorldStateSnapshot : ViRSESerializable
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

    public class WorldStateBundle : ViRSESerializable
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

    public class WorldStateWrapper : ViRSESerializable
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
