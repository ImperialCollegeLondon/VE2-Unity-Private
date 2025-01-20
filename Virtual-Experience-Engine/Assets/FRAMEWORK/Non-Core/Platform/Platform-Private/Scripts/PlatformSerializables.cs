using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using static NonCoreCommonSerializables;
using static VE2.Common.CommonSerializables;


#if UNITY_EDITOR
using UnityEngine;
#endif

namespace VE2.PlatformNetworking
{
    public class PlatformSerializables
    {
        public static readonly int PlatformNetcodeVersion = 1;


        public enum PlatformNetworkingMessageCodes
        {
            NetcodeVersionConfirmation,
            ServerRegistrationRequest,
            ServerRegistrationConfirmation,
            GlobalInfo,
            InstanceAllocationRequest,
            UpdateUserSettings,
        }


        public class ServerRegistrationRequest : VE2Serializable
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


        [Serializable]
        public class UserIdentity : VE2Serializable
        {
#if UNITY_EDITOR
            [SerializeField, NotNull]
#endif
            private string domain;

#if UNITY_EDITOR
            [SerializeField, NotNull]
#endif
            private string accountID;

#if UNITY_EDITOR
            [SerializeField, NotNull]
#endif
            private string firstName;

#if UNITY_EDITOR
            [SerializeField, NotNull]
#endif
            private string lastName;

#if UNITY_EDITOR
            [SerializeField, NotNull]
#endif
            private string machineName;

            public string Domain => domain;
            public string AccountID => accountID;
            public string FirstName => firstName;
            public string LastName => lastName;
            public string MachineName => machineName;

            public const string GuestID = "GUEST";

            public UserIdentity(string domain, string accountID, string firstName, string lastName, string machineName)
            {
                this.domain = domain;
                this.accountID = accountID;
                this.firstName = firstName;
                this.lastName = lastName;
                this.machineName = machineName;
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
                writer.Write(MachineName);

                return stream.ToArray();
            }

            protected override void PopulateFromBytes(byte[] data)
            {
                using MemoryStream stream = new(data);
                using BinaryReader reader = new(stream);

                this.domain = reader.ReadString();
                this.accountID = reader.ReadString();
                this.firstName = reader.ReadString();
                this.lastName = reader.ReadString();
                this.machineName = reader.ReadString();
            }

            public override string ToString()
            {
                return $"Domain: {domain}, AccountID: {accountID}, FirstName: {firstName}, LastName: {lastName} MachineName: {machineName})";
            }
        }


        public class ServerRegistrationConfirmation : VE2Serializable
        {
            public ushort LocalClientID { get; private set; }
            public UserSettingsPersistable UserSettings { get; private set; }
            public GlobalInfo GlobalInfo { get; private set; }
            public Dictionary<string, WorldDetails> AvailableWorlds { get; private set; }
            public bool CompletedTutorial { get; private set; }
            public string FTPIPAddress { get; private set; }
            public ushort FTPPortNumber { get; private set; }
            public string FTPUsername { get; private set; }
            public string FTPPassword { get; private set; }

            public ServerRegistrationConfirmation() { }

            public ServerRegistrationConfirmation(byte[] bytes) : base(bytes) { }

            public ServerRegistrationConfirmation(ushort localClientID, UserSettingsPersistable userSettings, GlobalInfo globalInfo, Dictionary<string, WorldDetails> availableWorlds, bool completedTutporial, string ftpIPAddress, ushort ftpPortNumber, string ftpUsername, string ftpPassword)
            {
                LocalClientID = localClientID;
                UserSettings = userSettings;
                GlobalInfo = globalInfo;
                AvailableWorlds = availableWorlds;
                CompletedTutorial = completedTutporial;
                FTPIPAddress = ftpIPAddress;
                FTPPortNumber = ftpPortNumber;
                FTPUsername = ftpUsername;
                FTPPassword = ftpPassword;
            }

            protected override byte[] ConvertToBytes()
            {
                using MemoryStream stream = new();
                using BinaryWriter writer = new(stream);

                writer.Write(LocalClientID);

                // Serialize PlayerPresentationConfig
                byte[] userSettingsBytes = UserSettings.Bytes;
                writer.Write((ushort)userSettingsBytes.Length);
                writer.Write(userSettingsBytes);

                // Serialize GlobalInfo
                byte[] globalInfoBytes = GlobalInfo.Bytes;
                writer.Write((ushort)globalInfoBytes.Length);
                writer.Write(globalInfoBytes);

                // Serialize AvailableWorlds
                writer.Write((ushort)AvailableWorlds.Count);
                foreach (var kvp in AvailableWorlds)
                {
                    writer.Write(kvp.Key);
                    byte[] worldDetailsBytes = kvp.Value.Bytes;
                    writer.Write((ushort)worldDetailsBytes.Length);
                    writer.Write(worldDetailsBytes);
                }

                // Serialize CompletedTutporial
                writer.Write(CompletedTutorial);

                writer.Write(FTPIPAddress);
                writer.Write(FTPPortNumber);
                writer.Write(FTPUsername);
                writer.Write(FTPPassword);

                return stream.ToArray();
            }

            protected override void PopulateFromBytes(byte[] data)
            {
                using MemoryStream stream = new(data);
                using BinaryReader reader = new(stream);

                LocalClientID = reader.ReadUInt16();

                // Deserialize PlayerPresentationConfig
                int userSettingsBytesLength = reader.ReadUInt16();
                byte[] userSettingsBytes = reader.ReadBytes(userSettingsBytesLength);
                UserSettings = new UserSettingsPersistable(userSettingsBytes);

                // Deserialize GlobalInfo
                int globalInfoLength = reader.ReadUInt16();
                byte[] globalInfoBytes = reader.ReadBytes(globalInfoLength);
                GlobalInfo = new GlobalInfo(globalInfoBytes);

                // Deserialize AvailableWorlds
                int availableWorldsCount = reader.ReadUInt16();
                AvailableWorlds = new Dictionary<string, WorldDetails>();
                for (int i = 0; i < availableWorldsCount; i++)
                {
                    string key = reader.ReadString();
                    int worldDetailsLength = reader.ReadUInt16();
                    byte[] worldDetailsBytes = reader.ReadBytes(worldDetailsLength);
                    WorldDetails worldDetails = new WorldDetails(worldDetailsBytes);
                    AvailableWorlds[key] = worldDetails;
                }

                // Deserialize CompletedTutporial
                CompletedTutorial = reader.ReadBoolean();

                // FTPIPAddress = reader.ReadString();
                // FTPPortNumber = reader.ReadUInt16();
                // FTPUsername = reader.ReadString();
                // FTPPassword = reader.ReadString();
            }
        }


        public class WorldDetails : VE2Serializable
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


        public class GlobalInfo : VE2Serializable
        {
            public Dictionary<string, PlatformInstanceInfo> InstanceInfos { get; private set; }

            public GlobalInfo(byte[] bytes) : base(bytes) { }

            public GlobalInfo(Dictionary<string, PlatformInstanceInfo> instanceInfos)
            {
                InstanceInfos = instanceInfos;
            }

            public GlobalInfo()
            {
                InstanceInfos = new Dictionary<string, PlatformInstanceInfo>();
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

                ushort instanceInfoCount = reader.ReadUInt16();
                InstanceInfos = new Dictionary<string, PlatformInstanceInfo>();

                for (int i = 0; i < instanceInfoCount; i++)
                {
                    string instanceCode = reader.ReadString();
                    ushort instanceInfoBytesLength = reader.ReadUInt16();
                    byte[] instanceInfoBytes = reader.ReadBytes(instanceInfoBytesLength);
                    PlatformInstanceInfo instanceInfo = new(instanceInfoBytes);
                    InstanceInfos[instanceCode] = instanceInfo;
                }
            }

        }


        public class PlatformInstanceInfo : InstanceInfoBase
        {
            public Dictionary<ushort, PlatformClientInfo> ClientInfos { get; private set; }

            public PlatformInstanceInfo()
            {
                ClientInfos = new Dictionary<ushort, PlatformClientInfo>();
            }

            public PlatformInstanceInfo(byte[] bytes) : base(bytes) { }

            public PlatformInstanceInfo(string worldName, string instanceSuffix, Dictionary<ushort, PlatformClientInfo> clientInfos) : base(worldName, instanceSuffix)
            {
                ClientInfos = clientInfos;
            }

            protected override byte[] ConvertToBytes()
            {
                using MemoryStream stream = new();
                using BinaryWriter writer = new(stream);

                byte[] baseBytes = base.ConvertToBytes();
                writer.Write((ushort)baseBytes.Length);
                writer.Write(baseBytes);

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

            protected override void PopulateFromBytes(byte[] data)
            {
                using MemoryStream stream = new(data);
                using BinaryReader reader = new(stream);

                ushort baseLength = reader.ReadUInt16();
                byte[] baseData = reader.ReadBytes(baseLength);
                base.PopulateFromBytes(baseData);

                int clientCount = reader.ReadUInt16();
                ClientInfos = new Dictionary<ushort, PlatformClientInfo>(clientCount);
                for (int i = 0; i < clientCount; i++)
                {
                    ushort key = reader.ReadUInt16();
                    ushort length = reader.ReadUInt16();
                    byte[] clientInfoBytes = reader.ReadBytes(length);
                    PlatformClientInfo value = new PlatformClientInfo(clientInfoBytes);
                    ClientInfos.Add(key, value);
                }
            }
        }


        public class PlatformClientInfo : ClientInfoBase
        {
            public PlayerPresentationConfig PlayerPresentationConfig;

            public PlatformClientInfo() { }

            public PlatformClientInfo(byte[] bytes) : base(bytes) { }

            public PlatformClientInfo(ushort id, bool isAdmin, string machineName, PlayerPresentationConfig playerPresentationConfig) : base(id, isAdmin, machineName)
            {
                PlayerPresentationConfig = playerPresentationConfig;
            }

            protected override byte[] ConvertToBytes()
            {
                using MemoryStream stream = new();
                using BinaryWriter writer = new(stream);

                byte[] baseBytes = base.ConvertToBytes();
                writer.Write((ushort)baseBytes.Length);
                writer.Write(baseBytes);

                byte[] playerPresentationConfigBytes = PlayerPresentationConfig.Bytes;
                writer.Write((ushort)playerPresentationConfigBytes.Length);
                writer.Write(playerPresentationConfigBytes);

                return stream.ToArray();
            }

            protected override void PopulateFromBytes(byte[] data)
            {
                using MemoryStream stream = new(data);
                using BinaryReader reader = new(stream);

                ushort baseBytesLength = reader.ReadUInt16();
                byte[] baseData = reader.ReadBytes(baseBytesLength);
                base.PopulateFromBytes(baseData);

                ushort playerPresentationConfigLength = reader.ReadUInt16();
                byte[] playerPresentationConfigBytes = reader.ReadBytes(playerPresentationConfigLength);
                PlayerPresentationConfig = new PlayerPresentationConfig(playerPresentationConfigBytes);
            }
        }


        public class InstanceAllocationRequest : VE2Serializable
        {
            public string WorldName { get; private set; }
            public string InstanceSuffix { get; private set; }
            public string InstanceCode => $"{WorldName}-{InstanceSuffix}";

            public InstanceAllocationRequest(byte[] bytes) : base(bytes) { }

            public InstanceAllocationRequest(string worldName, string instanceSuffix)
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

            protected override void PopulateFromBytes(byte[] data)
            {
                using MemoryStream stream = new(data);
                using BinaryReader reader = new(stream);

                WorldName = reader.ReadString();
                InstanceSuffix = reader.ReadString();
            }

        }
    }
}
