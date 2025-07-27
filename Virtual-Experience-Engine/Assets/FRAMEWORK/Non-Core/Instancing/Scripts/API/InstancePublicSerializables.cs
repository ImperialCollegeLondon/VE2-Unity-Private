using System;
using System.Collections.Generic;
using System.IO;
using static VE2.Common.Shared.CommonSerializables;
using static VE2.Core.Player.API.PlayerSerializables;
using static VE2.NonCore.Platform.API.PlatformPublicSerializables;

namespace VE2.NonCore.Instancing.API
{
    internal class InstancePublicSerializables
    {
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

            public InstancedInstanceInfo(InstanceCode instanceCode, ushort hostID, bool instanceMuted, Dictionary<ushort, InstancedClientInfo> clientInfos) : base(instanceCode)
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

        public class AvatarAppearanceWrapper : VE2Serializable //TODO: Should probably live in player serializables?
        {
            public bool UsingFrameworkPlayer;
            public InstancedAvatarAppearance InstancedAvatarAppearance;

            public AvatarAppearanceWrapper() { }

            public AvatarAppearanceWrapper(byte[] bytes) : base(bytes) { }

            public AvatarAppearanceWrapper(bool usingFrameworkAvatar, InstancedAvatarAppearance frameworkAvatarAppearance)
            {
                UsingFrameworkPlayer = usingFrameworkAvatar;
                InstancedAvatarAppearance = frameworkAvatarAppearance;
            }

            protected override byte[] ConvertToBytes()
            {
                using MemoryStream stream = new();
                using BinaryWriter writer = new(stream);

                writer.Write(UsingFrameworkPlayer);

                if (UsingFrameworkPlayer)
                {
                    byte[] virseAppearanceBytes = InstancedAvatarAppearance.Bytes;
                    writer.Write((ushort)virseAppearanceBytes.Length);
                    writer.Write(virseAppearanceBytes);
                }

                return stream.ToArray();
            }

            protected override void PopulateFromBytes(byte[] bytes)
            {
                using MemoryStream stream = new(bytes);
                using BinaryReader reader = new(stream);

                UsingFrameworkPlayer = reader.ReadBoolean();

                if (UsingFrameworkPlayer)
                {
                    ushort virseAppearanceBytesLength = reader.ReadUInt16();
                    byte[] virseAppearanceBytes = reader.ReadBytes(virseAppearanceBytesLength);
                    InstancedAvatarAppearance = new(virseAppearanceBytes);
                }
            }
        }
    }

}
