using System.Collections;
using System.Collections.Generic;
using System.IO;
using static NonCoreCommonSerializables;
using static VE2.Common.CommonSerializables;


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
        PingMessage
    }

    public class PingMessage : VE2Serializable
    {
        public int PingId { get; private set; }
        public ushort ClientId { get; private set; }

        public PingMessage(byte[] bytes) : base(bytes) { }

        public PingMessage(int pingId, ushort clientId, bool fromHost)
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

    //So what actually is this registration request?
    //Needs to have the avatar presentation details
    //Also needs the instance code 

    public class ServerRegistrationRequest : VE2Serializable
    {
        public string InstanceCode { get; private set; }
        public ushort IDToRestore { get; private set; }

        public ServerRegistrationRequest(byte[] bytes) : base(bytes) { }

        public ServerRegistrationRequest(string instanceCode, ushort idToRestore = ushort.MaxValue)
        {
            InstanceCode = instanceCode;
            IDToRestore = idToRestore;
        }

        protected override byte[] ConvertToBytes()
        {
            using MemoryStream stream = new();
            using BinaryWriter writer = new(stream);

            writer.Write(InstanceCode);
            writer.Write(IDToRestore);

            return stream.ToArray();
        }

        protected override void PopulateFromBytes(byte[] bytes)
        {
            using MemoryStream stream = new(bytes);
            using BinaryReader reader = new(stream);

            InstanceCode = reader.ReadString();
            IDToRestore = reader.ReadUInt16();
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
                byte[] clientInfoBytes = kvp.Value.Bytes;
                writer.Write((ushort)clientInfoBytes.Length);
                writer.Write(clientInfoBytes);
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

        public override bool Equals(object obj)
        {
            if (obj is not InstancedInstanceInfo other)
                return false;

            if (HostID != other.HostID || InstanceMuted != other.InstanceMuted || ClientInfos.Count != other.ClientInfos.Count)
                return false;

            foreach (var kvp in ClientInfos)
            {
                if (!other.ClientInfos.TryGetValue(kvp.Key, out var otherClientInfo) || !kvp.Value.Equals(otherClientInfo))
                    return false;
            }

            return true;
        }

        public override int GetHashCode()
        {
            int hashCode = base.GetHashCode();
            hashCode = (hashCode * 397) ^ HostID.GetHashCode();
            hashCode = (hashCode * 397) ^ InstanceMuted.GetHashCode();
            foreach (var kvp in ClientInfos)
            {
                hashCode = (hashCode * 397) ^ kvp.Key.GetHashCode();
                hashCode = (hashCode * 397) ^ kvp.Value.GetHashCode();
            }
            return hashCode;
        }
    }

    public class InstancedClientInfo : ClientInfoBase
    {
        public AvatarAppearanceWrapper AvatarAppearanceWrapper;

        public InstancedClientInfo() { }

        public InstancedClientInfo(byte[] bytes) : base(bytes) { }

        public InstancedClientInfo(ushort clientID, bool isAdmin, AvatarAppearanceWrapper instancedAvatarAppearance) : base(clientID, isAdmin, "unknown") //TODO, machine name should maybe be platform-specific?
        {
            AvatarAppearanceWrapper = instancedAvatarAppearance;
        }

        protected override byte[] ConvertToBytes()
        {
            using MemoryStream stream = new();
            using BinaryWriter writer = new(stream);

            byte[] baseBytes = base.ConvertToBytes();
            writer.Write((ushort)baseBytes.Length);
            writer.Write(baseBytes);

            byte[] avatarAppearanceBytes = AvatarAppearanceWrapper.Bytes;
            writer.Write((ushort)avatarAppearanceBytes.Length);
            writer.Write(avatarAppearanceBytes);

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
            AvatarAppearanceWrapper = new AvatarAppearanceWrapper(reader.ReadBytes(avatarAppearanceLength));
        }
    }

    public class AvatarAppearanceWrapper : VE2Serializable
    {
        public bool UsingViRSEPlayer;
        public AvatarAppearance ViRSEAvatarAppearance;

        public AvatarAppearanceWrapper() { }

        public AvatarAppearanceWrapper(byte[] bytes) : base(bytes) { }

        public AvatarAppearanceWrapper(bool usingViRSEAvatar, AvatarAppearance virseAvatarAppearance)
        {
            UsingViRSEPlayer = usingViRSEAvatar;
            ViRSEAvatarAppearance = virseAvatarAppearance;
        }

        protected override byte[] ConvertToBytes()
        {
            using MemoryStream stream = new();
            using BinaryWriter writer = new(stream);

            writer.Write(UsingViRSEPlayer);

            if (UsingViRSEPlayer)
            {
                byte[] virseAppearanceBytes = ViRSEAvatarAppearance.Bytes;
                writer.Write((ushort)virseAppearanceBytes.Length);
                writer.Write(virseAppearanceBytes);
            }

            return stream.ToArray();
        }

        protected override void PopulateFromBytes(byte[] bytes)
        {
            using MemoryStream stream = new(bytes);
            using BinaryReader reader = new(stream);

            UsingViRSEPlayer = reader.ReadBoolean();
            
            if (UsingViRSEPlayer)
            {
                ushort virseAppearanceBytesLength = reader.ReadUInt16();
                byte[] virseAppearanceBytes = reader.ReadBytes(virseAppearanceBytesLength);
                ViRSEAvatarAppearance = new(virseAppearanceBytes);
            }
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
