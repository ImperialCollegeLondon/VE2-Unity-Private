using System;
using System.Collections.Generic;
using System.IO;
using static VE2.Common.Shared.CommonSerializables;
using static VE2.Core.Player.API.PlayerSerializables;

namespace VE2.NonCore.Platform.API
{
    internal class PlatformPublicSerializables
    {
        internal class NetcodeVersionConfirmation : VE2Serializable
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


        [Serializable]
        internal class ServerConnectionSettings : VE2Serializable
        {
            public string Username = "";
            public string Password = "";
            public string ServerAddress = "";
            public ushort ServerPort;

            public ServerConnectionSettings() { }

            public ServerConnectionSettings(string username, string password, string serverAddress, ushort serverPort)
            {
                Username = username;
                Password = password;
                ServerAddress = serverAddress;
                ServerPort = serverPort;
            }

            public ServerConnectionSettings(byte[] bytes) : base(bytes) { }

            protected override byte[] ConvertToBytes()
            {
                using MemoryStream stream = new();
                using BinaryWriter writer = new(stream);

                writer.Write(Username);
                writer.Write(Password);
                writer.Write(ServerAddress);
                writer.Write(ServerPort);

                return stream.ToArray();
            }
            protected override void PopulateFromBytes(byte[] data)
            {
                using MemoryStream stream = new(data);
                using BinaryReader reader = new(stream);

                Username = reader.ReadString();
                Password = reader.ReadString();
                ServerAddress = reader.ReadString();
                ServerPort = reader.ReadUInt16();
            }
        }


        internal class InstanceInfoBase : VE2Serializable
        {
            public InstanceCode InstanceCode { get; private set; }

            public InstanceInfoBase() { }

            public InstanceInfoBase(byte[] bytes) : base(bytes) { }

            public InstanceInfoBase(InstanceCode instanceCode)
            {
                InstanceCode = instanceCode;
            }

            protected override byte[] ConvertToBytes()
            {
                using MemoryStream stream = new();
                using BinaryWriter writer = new(stream);

                writer.Write(InstanceCode.ToString());

                return stream.ToArray();
            }

            protected override void PopulateFromBytes(byte[] bytes)
            {
                using MemoryStream stream = new(bytes);
                using BinaryReader reader = new(stream);

                InstanceCode = new InstanceCode(reader.ReadString());
            }
        }

        internal class InstanceCode //: VE2Serializable
        {
            public string WorldName { get; private set; }
            public string InstanceSuffix { get; private set; }
            public ushort VersionNumber { get; private set; }

            public InstanceCode() { }

            //public InstanceCode(byte[] bytes) : base(bytes) { }

            public InstanceCode(string worldName, string instanceSuffix, ushort versionNumber)
            {
                WorldName = worldName;
                InstanceSuffix = instanceSuffix;
                VersionNumber = versionNumber;
            }

            public InstanceCode(string instanceCodeString)
            {
                // Expected format: WorldName-InstanceSuffix-VersionNumber
                string[] parts = instanceCodeString.Split('-');
                if (parts.Length != 3)
                    throw new ArgumentException("Invalid instance code format. Expected format: WorldName-InstanceSuffix-VersionNumber. Received: " + instanceCodeString);

                WorldName = parts[0];
                InstanceSuffix = parts[1];
                if (ushort.TryParse(parts[2], out ushort version))
                    VersionNumber = version;
                else
                    throw new ArgumentException($"Invalid version number ({parts[2]}) in instance code ({instanceCodeString})");
            }

            public override string ToString() => $"{WorldName}-{InstanceSuffix}-{VersionNumber}";

            // protected override byte[] ConvertToBytes()
            // {
            //     using MemoryStream stream = new();
            //     using BinaryWriter writer = new(stream);

            //     writer.Write(WorldName);
            //     writer.Write(InstanceSuffix);
            //     writer.Write((ushort)VersionNumber);

            //     return stream.ToArray();
            // }

            // protected override void PopulateFromBytes(byte[] bytes)
            // {
            //     using MemoryStream stream = new(bytes);
            //     using BinaryReader reader = new(stream);

            //     WorldName = reader.ReadString();
            //     InstanceSuffix = reader.ReadString();
            //     VersionNumber = reader.ReadUInt16();
            // }

            public override bool Equals(object obj)
            {
                if (obj is InstanceCode other)
                    return ToString().Equals(other.ToString());
                else
                    return false;
            }
        }


        internal abstract class ClientInfoBase : VE2Serializable
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

        internal class PlatformInstanceInfo : InstanceInfoBase
        {
            public Dictionary<ushort, PlatformClientInfo> ClientInfos { get; private set; }

            public PlatformInstanceInfo()
            {
                ClientInfos = new Dictionary<ushort, PlatformClientInfo>();
            }

            public PlatformInstanceInfo(byte[] bytes) : base(bytes) { }

            public PlatformInstanceInfo(InstanceCode instanceCode, Dictionary<ushort, PlatformClientInfo> clientInfos) : base(instanceCode)
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

        [Serializable]
        internal class WorldDetails : VE2Serializable
        {
            //Note, these are public writable only so the JSON utility can write to them when reading the config file
            public string Name;
            public int VersionNumber;
            public bool HasCustomFTPServer;
            public ServerConnectionSettings CustomFTPServerSettings;
            public bool HasCustomInstanceServer;
            public ServerConnectionSettings CustomInstanceServerSettings;

            public WorldDetails() { }

            public WorldDetails(byte[] bytes) : base(bytes) { }

            public WorldDetails(string name, int versionNumber, bool hasCustomFTPServerSettings, ServerConnectionSettings customFTPServerSettings, bool hasCustomInstanceServerSettings, ServerConnectionSettings customInstanceServerSettings)
            {
                Name = name;
                VersionNumber = versionNumber;
                HasCustomFTPServer = hasCustomFTPServerSettings;
                CustomFTPServerSettings = customFTPServerSettings;
                HasCustomInstanceServer = hasCustomInstanceServerSettings;
                CustomInstanceServerSettings = customInstanceServerSettings;
            }


            protected override byte[] ConvertToBytes()
            {
                using MemoryStream stream = new();
                using BinaryWriter writer = new(stream);

                Console.WriteLine("\n\nWrite world " + Name);

                writer.Write(Name);
                writer.Write(VersionNumber);

                writer.Write(HasCustomFTPServer);
                if (HasCustomFTPServer)
                {
                    byte[] customFTPServerSettingsBytes = CustomFTPServerSettings.Bytes;
                    writer.Write((ushort)customFTPServerSettingsBytes.Length);
                    writer.Write(customFTPServerSettingsBytes);
                }

                writer.Write(HasCustomInstanceServer);
                if (HasCustomInstanceServer)
                {
                    byte[] customInstanceServerSettingsBytes = CustomInstanceServerSettings.Bytes;
                    writer.Write((ushort)customInstanceServerSettingsBytes.Length);
                    writer.Write(customInstanceServerSettingsBytes);
                }

                return stream.ToArray();
            }

            protected override void PopulateFromBytes(byte[] data)
            {
                using MemoryStream stream = new(data);
                using BinaryReader reader = new(stream);

                Name = reader.ReadString();
                VersionNumber = reader.ReadInt32();

                HasCustomFTPServer = reader.ReadBoolean();
                if (HasCustomFTPServer)
                {
                    ushort customFTPServerSettingsLength = reader.ReadUInt16();
                    byte[] customFTPServerSettingsBytes = reader.ReadBytes(customFTPServerSettingsLength);
                    CustomFTPServerSettings = new ServerConnectionSettings(customFTPServerSettingsBytes);
                }

                HasCustomInstanceServer = reader.ReadBoolean();
                if (HasCustomInstanceServer)
                {
                    ushort customInstanceServerSettingsLength = reader.ReadUInt16();
                    byte[] customInstanceServerSettingsBytes = reader.ReadBytes(customInstanceServerSettingsLength);
                    CustomInstanceServerSettings = new ServerConnectionSettings(customInstanceServerSettingsBytes);
                }

            }
        }

        [Serializable]
        public class AdminUpdateNotice : VE2Serializable
        {
            public ushort ClientID { get; private set; }
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

                writer.Write(ClientID);
                writer.Write(IsAdmin);
                return stream.ToArray();
            }

            protected override void PopulateFromBytes(byte[] bytes)
            {
                using MemoryStream stream = new(bytes);
                using BinaryReader reader = new(stream);

                ClientID = reader.ReadUInt16();
                IsAdmin = reader.ReadBoolean();
            }
        }
    }

}
