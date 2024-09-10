using System.Collections;
using System.Collections.Generic;
using System.IO;
using ViRSE.Core.Shared;
using static NonCoreCommonSerializables;
using static ViRSE.Core.Shared.CoreCommonSerializables;

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
    }

    //So what actually is this registration request?
    //Needs to have the avatar presentation details
    //Also needs the instance code 

    public class ServerRegistrationRequest : ViRSESerializable
    {
        public string InstanceCode { get; private set; }
        public InstancedAvatarAppearance AvatarDetails { get; private set; }

        public ServerRegistrationRequest(byte[] bytes) : base(bytes) { }

        public ServerRegistrationRequest(InstancedAvatarAppearance avatarDetails, string instanceCode)
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
            AvatarDetails = new InstancedAvatarAppearance(reader.ReadBytes(avatarDetailsLength));
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

    public class InstancedAvatarAppearance : PlayerPresentationConfig
    {
        public bool UsingViRSEAvatar = true;
        public string AvatarHeadTypeOverride;
        public string AvatarBodyTypeOverride;
        public bool AvatarTransparancy = true;

        public InstancedAvatarAppearance() { }

        public InstancedAvatarAppearance(byte[] bytes) : base(bytes) { }

        public InstancedAvatarAppearance(string playerName, string avatarHeadType, string avatarBodyType, ushort avatarRed, ushort avatarGreen, ushort avatarBlue, bool usingViRSEAvatar, string avatarHeadTypeOverride, string avatarBodyTypeOverride, bool avatarTransparancy) 
            : base(playerName, avatarHeadType, avatarBodyType, avatarRed, avatarGreen, avatarBlue)
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

            // Call base class method to serialize base class fields
            writer.Write(base.ConvertToBytes());

            // Serialize derived class fields
            writer.Write(UsingViRSEAvatar);
            writer.Write(AvatarHeadTypeOverride);
            writer.Write(AvatarBodyTypeOverride);
            writer.Write(AvatarTransparancy);

            return stream.ToArray();
        }

        protected override void PopulateFromBytes(byte[] data)
        {
            using MemoryStream stream = new(data);
            using BinaryReader reader = new(stream);

            // Deserialize base class fields
            byte[] baseData = reader.ReadBytes((int)(stream.Length - stream.Position));
            base.PopulateFromBytes(baseData);

            // Deserialize derived class fields
            UsingViRSEAvatar = reader.ReadBoolean();
            AvatarHeadTypeOverride = reader.ReadString();
            AvatarBodyTypeOverride = reader.ReadString();
            AvatarTransparancy = reader.ReadBoolean();
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

        public InstancedInstanceInfo(byte[] bytes) : base(bytes)
        {
            ClientInfos = new Dictionary<ushort, InstancedClientInfo>();
        }

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
            writer.Write(base.ConvertToBytes());

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

            byte[] baseData = reader.ReadBytes((int)(stream.Length - stream.Position));
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
        public InstancedAvatarAppearance InstancedAvatarAppearance { get; private set; }

        public InstancedClientInfo() { }

        public InstancedClientInfo(byte[] bytes) : base(bytes) { }

        public InstancedClientInfo(ushort clientID, /*string displayName,*/ bool isAdmin, string machineName, InstancedAvatarAppearance instancedAvatarAppearance)
        {
            ClientID = clientID;
            //DisplayName = displayName;
            IsAdmin = isAdmin;
            MachineName = machineName;
            InstancedAvatarAppearance = instancedAvatarAppearance;
        }

        protected override byte[] ConvertToBytes()
        {
            using MemoryStream stream = new();
            using BinaryWriter writer = new(stream);

            // Serialize base class fields
            writer.Write(base.ConvertToBytes());

            // Serialize derived class fields
            writer.Write((ushort)InstancedAvatarAppearance.Bytes.Length);
            writer.Write(InstancedAvatarAppearance.Bytes);

            return stream.ToArray();
        }

        protected override void PopulateFromBytes(byte[] bytes)
        {
            using MemoryStream stream = new(bytes);
            using BinaryReader reader = new(stream);

            // Deserialize base class fields
            byte[] baseData = reader.ReadBytes((int)(stream.Length - stream.Position));
            base.PopulateFromBytes(baseData);

            // Deserialize derived class fields
            ushort avatarAppearanceLength = reader.ReadUInt16();
            InstancedAvatarAppearance = new InstancedAvatarAppearance(reader.ReadBytes(avatarAppearanceLength));
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
