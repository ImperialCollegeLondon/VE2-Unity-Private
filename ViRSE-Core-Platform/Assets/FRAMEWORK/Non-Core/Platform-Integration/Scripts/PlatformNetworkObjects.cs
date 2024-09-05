using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using ViRSE.Core.Shared;
using static CommonNetworkObjects;

public class PlatformNetworkObjects
{
    public static readonly int PlatformNetcodeVersion = 5;

    public enum PlatformNetworkingMessageCodes
    {
        NetcodeVersionConfirmation,
        ServerRegistrationRequest,
        ServerRegistrationConfirmation,
        GlobalInfo,
        InstanceAllocationRequest
    }

    public class ServerRegistrationRequest : ViRSESerializable
    {
        public UserIdentity UserIdentity { get; private set; }
        public string StartingInstanceCode { get; private set; }

        public ServerRegistrationRequest(UserIdentity userIdentity, string startingInstanceCode)
        {
            UserIdentity = userIdentity;
            StartingInstanceCode = startingInstanceCode;
        }

        public ServerRegistrationRequest(byte[] bytes) : base(bytes) { }

        protected override byte[] ConvertToBytes()
        {
            using MemoryStream stream = new();
            using BinaryWriter writer = new(stream);

            byte[] userIdentityBytes = UserIdentity.Bytes;
            writer.Write((ushort)userIdentityBytes.Length); 
            writer.Write(userIdentityBytes); 
            writer.Write(StartingInstanceCode);

            return stream.ToArray();
        }
        protected override void PopulateFromBytes(byte[] data)
        {
            using MemoryStream stream = new(data);
            using BinaryReader reader = new(stream);

            int userIdentityLength = reader.ReadUInt16();
            byte[] userIdentityBytes = reader.ReadBytes(userIdentityLength);
            UserIdentity = new UserIdentity(userIdentityBytes);

            StartingInstanceCode = reader.ReadString();
        }
    }

    public class UserIdentity : ViRSESerializable
    {
        public string Domain { get; private set; }
        public string AccountID { get; private set; }
        public string FirstName { get; private set; }
        public string LastName { get; private set; }

        public UserIdentity(string domain, string accountID, string firstName, string lastName)
        {
            Domain = domain;
            AccountID = accountID;
            FirstName = firstName;
            LastName = lastName;
        }

        public UserIdentity(byte[] bytes) : base(bytes) { }

        protected override byte[] ConvertToBytes()
        {
            using MemoryStream stream = new();
            using BinaryWriter writer = new(stream);

            writer.Write(Domain);
            writer.Write(AccountID);
            writer.Write(FirstName);
            writer.Write(LastName);

            return stream.ToArray();
        }

        protected override void PopulateFromBytes(byte[] data)
        {
            using MemoryStream stream = new(data);
            using BinaryReader reader = new(stream);

            Domain = reader.ReadString();
            AccountID = reader.ReadString();
            FirstName = reader.ReadString();
            LastName = reader.ReadString();
        }
    }


    public class ServerRegistrationConfirmation : ViRSESerializable
    {
        public ushort LocalClientID { get; private set; }
        public GlobalInfo GlobalInfo { get; private set; }
        public Dictionary<string, WorldDetails> AvailableWorlds { get; private set; }

        public ServerRegistrationConfirmation(ushort localClientID, GlobalInfo globalInfo, Dictionary<string, WorldDetails> availableWorlds)
        {
            LocalClientID = localClientID;
            GlobalInfo = globalInfo;
            AvailableWorlds = availableWorlds;
        }

        public ServerRegistrationConfirmation(byte[] bytes) : base(bytes){ }

        protected override byte[] ConvertToBytes()
        {
            using MemoryStream stream = new();
            using BinaryWriter writer = new(stream);

            writer.Write(LocalClientID);


            byte[] globalInfoBytes = GlobalInfo.Bytes;
            writer.Write((ushort)globalInfoBytes.Length);
            writer.Write(globalInfoBytes);

            writer.Write((ushort)AvailableWorlds.Count);
            foreach (var kvp in AvailableWorlds)
            {
                writer.Write(kvp.Key);
                byte[] worldBytes = kvp.Value.Bytes;
                writer.Write((ushort)worldBytes.Length);
                writer.Write(worldBytes);
            }

            return stream.ToArray();
        }

        protected override void PopulateFromBytes(byte[] data)
        {
            using MemoryStream stream = new(data);
            using BinaryReader reader = new(stream);

            LocalClientID = reader.ReadUInt16();

            int globalInfoLength = reader.ReadUInt16();
            byte[] globalInfoBytes = reader.ReadBytes(globalInfoLength);
            GlobalInfo = new GlobalInfo(globalInfoBytes);

            int availableWorldsCount = reader.ReadUInt16();
            AvailableWorlds = new Dictionary<string, WorldDetails>();
            for (int i = 0; i < availableWorldsCount; i++)
            {
                string key = reader.ReadString();
                int worldLength = reader.ReadUInt16();
                byte[] worldBytes = reader.ReadBytes(worldLength);
                AvailableWorlds[key] = new WorldDetails(worldBytes);
            }
        }
    }

    public class GlobalInfo : ViRSESerializable
    {
        public Dictionary<string, InstanceInfo> InstanceInfos { get; private set; }
        public GlobalInfo(byte[] bytes) : base(bytes) { }   
        
        public GlobalInfo(Dictionary<string, InstanceInfo> instanceInfos)
        {
            InstanceInfos = instanceInfos;
        }

        protected override byte[] ConvertToBytes()
        {
            using MemoryStream stream = new();
            using BinaryWriter writer = new(stream);

            writer.Write((ushort)InstanceInfos.Count);

            foreach (var kvp in InstanceInfos)
            {
                writer.Write(kvp.Key);
                byte[] instanceInfoBytes = kvp.Value.Bytes;
                writer.Write((ushort)instanceInfoBytes.Length);
                writer.Write(instanceInfoBytes);
            }

            return stream.ToArray();
        }

        protected override void PopulateFromBytes(byte[] data)
        {
            using MemoryStream stream = new(data);
            using BinaryReader reader = new(stream);

            int instanceInfoCount = reader.ReadUInt16();

            InstanceInfos = new Dictionary<string, InstanceInfo>();

            for (int i = 0; i < instanceInfoCount; i++)
            {
                string instanceCode = reader.ReadString();
                int instanceInfoBytesLength = reader.ReadUInt16();
                byte[] instanceInfoBytes = reader.ReadBytes(instanceInfoBytesLength);
                InstanceInfo instanceInfo = new(instanceInfoBytes);
                InstanceInfos[instanceCode] = instanceInfo;
            }
        }

    }

    public class WorldDetails : ViRSESerializable
    {
        public string Name { get; private set; }
        public string Subtitle { get; private set; }
        public string Authors { get; private set; }
        public string DateOfPublish { get; private set; }
        public int VersionNumber { get; private set; }
        public bool VREnabled { get; private set; }
        public bool TwoDEnabled { get; private set; }
        public bool MultiplayerEnabled { get; private set; }
        public string Path { get; private set; }
        public string IPAddress { get; private set; }
        public ushort PortNumber { get; private set; }
        public byte[] Thumbnail { get; private set; }

        public WorldDetails(string name, string subtitle, string authors, string dateOfPublish, int versionNumber, bool vrEnabled, bool twoDEnabled, bool multiplayerEnabled, string path, string ipAddress, ushort portNumber, byte[] thumbnail)
        {
            Name = name;
            Subtitle = subtitle;
            Authors = authors;
            DateOfPublish = dateOfPublish;
            VersionNumber = versionNumber;
            VREnabled = vrEnabled;
            TwoDEnabled = twoDEnabled;
            MultiplayerEnabled = multiplayerEnabled;
            Path = path;
            IPAddress = ipAddress;
            PortNumber = portNumber;
            Thumbnail = thumbnail;
        }

        public WorldDetails(byte[] bytes) : base(bytes) { }

        protected override byte[] ConvertToBytes()
        {
            using MemoryStream stream = new();
            using BinaryWriter writer = new(stream);

            writer.Write(Name);
            writer.Write(Subtitle);
            writer.Write(Authors);
            writer.Write(DateOfPublish);
            writer.Write(VersionNumber);
            writer.Write(VREnabled);
            writer.Write(TwoDEnabled);
            writer.Write(MultiplayerEnabled);
            writer.Write(Path);
            writer.Write(IPAddress);
            writer.Write(PortNumber);
            writer.Write(Thumbnail.Length);
            writer.Write(Thumbnail);

            return stream.ToArray();
        }

        protected override void PopulateFromBytes(byte[] data)
        {
            using MemoryStream stream = new(data);
            using BinaryReader reader = new(stream);

            Name = reader.ReadString();
            Subtitle = reader.ReadString();
            Authors = reader.ReadString();
            DateOfPublish = reader.ReadString();
            VersionNumber = reader.ReadInt32();
            VREnabled = reader.ReadBoolean();
            TwoDEnabled = reader.ReadBoolean();
            MultiplayerEnabled = reader.ReadBoolean();
            Path = reader.ReadString();
            IPAddress = reader.ReadString();
            PortNumber = reader.ReadUInt16();
            int thumbnailLength = reader.ReadInt32();
            Thumbnail = reader.ReadBytes(thumbnailLength);
        }
    }

    public class InstanceAllocationRequest : ViRSESerializable
    {
        public string InstanceCode { get; private set; }

        public InstanceAllocationRequest(byte[] bytes) : base(bytes) { }

        public InstanceAllocationRequest(string instanceCode)
        {
            InstanceCode = instanceCode;
        }

        protected override byte[] ConvertToBytes()
        {
            using MemoryStream stream = new();
            using BinaryWriter writer = new(stream);

            writer.Write(InstanceCode);

            return stream.ToArray();
        }

        protected override void PopulateFromBytes(byte[] data)
        {
            using MemoryStream stream = new(data);
            using BinaryReader reader = new(stream);

            InstanceCode = reader.ReadString();
        }

    }
}
