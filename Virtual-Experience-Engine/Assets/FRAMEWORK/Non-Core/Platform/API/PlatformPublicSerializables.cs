using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VE2.Platform.API
{
    using System.IO;
    using static NonCoreCommonSerializables;
    using static VE2.Common.CommonSerializables;

    public class PlatformPublicSerializables
    {
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

        [Serializable]
        internal class WorldDetails : VE2Serializable //TODO: We don't actually need to send this all via the interface, its just world names and versions we need 
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


        internal class GlobalInfo : VE2Serializable
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


        internal class PlatformInstanceInfo : InstanceInfoBase //TODO - shouldn't be in public serializables
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

    }

}
