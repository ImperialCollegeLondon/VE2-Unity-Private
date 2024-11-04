using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using static VE2.Common.CoreCommonSerializables;

public class NonCoreCommonSerializables
{
    public class NetcodeVersionConfirmation : VE2Serializable
    {
        public int NetcodeVersion { get; private set; }

        public NetcodeVersionConfirmation(byte[] bytes) : base(bytes) { }

        public NetcodeVersionConfirmation(int netcodeVersion)
        {
            NetcodeVersion = netcodeVersion;
        }

        protected override byte[] ConvertToBytes()
        {
            using MemoryStream stream = new();
            using BinaryWriter writer = new(stream);

            writer.Write(NetcodeVersion);

            return stream.ToArray();
        }

        protected override void PopulateFromBytes(byte[] bytes)
        {
            using MemoryStream stream = new(bytes);
            using BinaryReader reader = new(stream);

            NetcodeVersion = reader.ReadInt32();
        }
    }

    public class InstanceInfoBase : VE2Serializable
    {
        public string WorldName { get; private set; }
        public string InstanceSuffix { get; private set; }
        public string InstanceCode => GetInstanceCode(WorldName, InstanceSuffix);

        public static string GetInstanceCode(string worldName, string instanceSuffix) => $"{worldName}-{instanceSuffix}";
        public static (string, string) SplitInstanceCode(string instanceCode) => (instanceCode.Split('-')[0], instanceCode.Split('-')[1]);

        public InstanceInfoBase() { }

        public InstanceInfoBase(byte[] bytes) : base(bytes) { }

        public InstanceInfoBase(string worldName, string instanceSuffix)
        {
            WorldName = worldName;
            InstanceSuffix = instanceSuffix;
        }

        protected override byte[] ConvertToBytes()
        {
            using MemoryStream stream = new();
            using BinaryWriter writer = new(stream);

            writer.Write(WorldName);
            writer.Write(InstanceSuffix);

            return stream.ToArray();
        }

        protected override void PopulateFromBytes(byte[] bytes)
        {
            using MemoryStream stream = new(bytes);
            using BinaryReader reader = new(stream);

            WorldName = reader.ReadString();
            InstanceSuffix = reader.ReadString();
        }
    }


    public abstract class ClientInfoBase : VE2Serializable
    {
        public ushort ClientID;
        //public string DisplayName;
        public bool IsAdmin;
        public string MachineName; //TODO, this should maybe be platform-specific? 

        public ClientInfoBase() { }

        public ClientInfoBase(byte[] bytes) : base(bytes) { }

        protected ClientInfoBase(ushort clientID, bool isAdmin, string machineName)
        {
            ClientID = clientID;
            IsAdmin = isAdmin;
            MachineName = machineName;
        }

        protected override byte[] ConvertToBytes()
        {
            using MemoryStream stream = new();
            using BinaryWriter writer = new(stream);

            writer.Write(ClientID);
            //writer.Write(DisplayName);
            writer.Write(IsAdmin);
            writer.Write(MachineName);

            return stream.ToArray();
        }

        protected override void PopulateFromBytes(byte[] bytes)
        {
            using MemoryStream stream = new(bytes);
            using BinaryReader reader = new(stream);

            ClientID = reader.ReadUInt16();
            //DisplayName = reader.ReadString();
            IsAdmin = reader.ReadBoolean();
            MachineName = reader.ReadString();
        }
    }
}
