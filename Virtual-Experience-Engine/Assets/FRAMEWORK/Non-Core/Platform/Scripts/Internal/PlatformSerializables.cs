using System;
using System.Collections.Generic;
using System.IO;
using static VE2.NonCore.Platform.API.PlatformPublicSerializables;
using static VE2.Core.Player.API.PlayerSerializables;
using static VE2.Common.Shared.CommonSerializables;



#if UNITY_EDITOR
using UnityEngine;
#endif

namespace VE2.NonCore.Platform.Internal
{
    internal class PlatformSerializables
    {
        internal static readonly int PlatformNetcodeVersion = 1;

        public enum PlatformNetworkingMessageCodes
        {
            NetcodeVersionConfirmation,
            ServerRegistrationRequest,
            ServerRegistrationResponse,
            GlobalInfo,
            InstanceAllocationRequest,
            UpdatePlayerPresentation,
        }


        // public class FirstTimeAuthCheckRequest : VE2Serializable
        // {
        //     public string CustomerID;
        //     public string CustomerKey;

        //     public FirstTimeAuthCheckRequest(byte[] bytes) : base(bytes) { }

        //     public FirstTimeAuthCheckRequest(string customerID, string customerKey)
        //     {
        //         CustomerID = customerID;
        //         CustomerKey = customerKey;
        //     }

        //     protected override byte[] ConvertToBytes()
        //     {
        //         using MemoryStream stream = new();
        //         using BinaryWriter writer = new(stream);

        //         writer.Write(CustomerID);
        //         writer.Write(CustomerKey);

        //         return stream.ToArray();
        //     }

        //     protected override void PopulateFromBytes(byte[] data)
        //     {
        //         using MemoryStream stream = new(data);
        //         using BinaryReader reader = new(stream);

        //         CustomerID = reader.ReadString();
        //         CustomerKey = reader.ReadString();
        //     }
        // }

        // public class FirstTimeAuthCheckResponse : VE2Serializable
        // {
        //     public bool AuthSuccess { get; private set; }

        //     public FirstTimeAuthCheckResponse(byte[] bytes) : base(bytes) { }

        //     public FirstTimeAuthCheckResponse(bool authSuccess)
        //     {
        //         AuthSuccess = authSuccess;
        //     }

        //     protected override byte[] ConvertToBytes()
        //     {
        //         using MemoryStream stream = new();
        //         using BinaryWriter writer = new(stream);

        //         writer.Write(AuthSuccess);

        //         return stream.ToArray();
        //     }

        //     protected override void PopulateFromBytes(byte[] data)
        //     {
        //         using MemoryStream stream = new(data);
        //         using BinaryReader reader = new(stream);

        //         AuthSuccess = reader.ReadBoolean();
        //     }
        // }

        //If auto-connect, should send this message 
        //If manual-connect (which needs an API on the platform interface), just connect with id and key


        internal class ServerRegistrationRequest : VE2Serializable
        {
            public string CustomerID;
            private string CustomerKey;
            public string StartingInstanceCode { get; private set; }
            public PlayerPresentationConfig PlayerPresentationConfig;

            public ServerRegistrationRequest(string customerID, string customerKey, string startingInstanceCode, PlayerPresentationConfig playerPresentationConfig)
            {
                CustomerID = customerID;
                CustomerKey = customerKey;
                StartingInstanceCode = startingInstanceCode;
                PlayerPresentationConfig = playerPresentationConfig;
            }

            public ServerRegistrationRequest(byte[] bytes) : base(bytes) { }

            protected override byte[] ConvertToBytes()
            {
                using MemoryStream stream = new();
                using BinaryWriter writer = new(stream);

                writer.Write(CustomerID);
                writer.Write(CustomerKey);
                writer.Write(StartingInstanceCode);

                byte[] playerPresentationConfigBytes = PlayerPresentationConfig.Bytes;
                writer.Write((ushort)playerPresentationConfigBytes.Length);
                writer.Write(playerPresentationConfigBytes);

                return stream.ToArray();
            }
            protected override void PopulateFromBytes(byte[] data)
            {
                using MemoryStream stream = new(data);
                using BinaryReader reader = new(stream);

                CustomerID = reader.ReadString();
                CustomerKey = reader.ReadString();
                StartingInstanceCode = reader.ReadString();

                ushort playerPresentationConfigLength = reader.ReadUInt16();
                byte[] playerPresentationConfigBytes = reader.ReadBytes(playerPresentationConfigLength);
                PlayerPresentationConfig = new PlayerPresentationConfig(playerPresentationConfigBytes);
            }
        }


        internal class ServerRegistrationResponse : VE2Serializable
        {
            public bool AuthSuccess { get; private set; }
            public ushort LocalClientID { get; private set; }
            public GlobalInfo GlobalInfo { get; private set; }
            public Dictionary<string, WorldDetails> ActiveWorlds { get; private set; }
            public ServerConnectionSettings WorldBuildsFTPServerSettings;
            public ServerConnectionSettings DefaultWorldSubStoreFTPServerSettings;
            public ServerConnectionSettings DefaultInstanceServerSettings;

            public ServerRegistrationResponse() { }

            public ServerRegistrationResponse(byte[] bytes) : base(bytes) { }

            public ServerRegistrationResponse(bool authSuccess, ushort localClientID, GlobalInfo globalInfo, Dictionary<string, WorldDetails> availableWorlds,
                ServerConnectionSettings worldBuildsFTPServerSettings, ServerConnectionSettings defaultWorldSubStoreFTPServerSettings, ServerConnectionSettings defaultInstanceServerSettings)
            {
                AuthSuccess = authSuccess;
                LocalClientID = localClientID;
                GlobalInfo = globalInfo;
                ActiveWorlds = availableWorlds;
                WorldBuildsFTPServerSettings = worldBuildsFTPServerSettings;
                DefaultWorldSubStoreFTPServerSettings = defaultWorldSubStoreFTPServerSettings;
                DefaultInstanceServerSettings = defaultInstanceServerSettings;
            }

            protected override byte[] ConvertToBytes()
            {
                using MemoryStream stream = new();
                using BinaryWriter writer = new(stream);

                writer.Write(AuthSuccess);
                if (!AuthSuccess)
                    return stream.ToArray(); ;

                writer.Write(LocalClientID);

                byte[] globalInfoBytes = GlobalInfo.Bytes;
                writer.Write((ushort)globalInfoBytes.Length);
                writer.Write(globalInfoBytes);

                writer.Write((ushort)ActiveWorlds.Count);
                foreach (var kvp in ActiveWorlds)
                {
                    writer.Write(kvp.Key);
                    byte[] worldDetailsBytes = kvp.Value.Bytes;
                    writer.Write((ushort)worldDetailsBytes.Length);
                    writer.Write(worldDetailsBytes);
                }

                byte[] worldsStoreFTPNetworkSettingsBytes = WorldBuildsFTPServerSettings.Bytes;
                writer.Write((ushort)worldsStoreFTPNetworkSettingsBytes.Length);
                writer.Write(worldsStoreFTPNetworkSettingsBytes);


                byte[] defaultWorldSubStoreFTPServerSettingsBytes = DefaultWorldSubStoreFTPServerSettings.Bytes;
                writer.Write((ushort)defaultWorldSubStoreFTPServerSettingsBytes.Length);
                writer.Write(defaultWorldSubStoreFTPServerSettingsBytes);

                byte[] defaultInstanceServerSettingsBytes = DefaultInstanceServerSettings.Bytes;
                writer.Write((ushort)defaultInstanceServerSettingsBytes.Length);
                writer.Write(defaultInstanceServerSettingsBytes);

                return stream.ToArray();
            }

            protected override void PopulateFromBytes(byte[] data)
            {
                using MemoryStream stream = new(data);
                using BinaryReader reader = new(stream);

                AuthSuccess = reader.ReadBoolean();
                if (!AuthSuccess)
                    return;

                LocalClientID = reader.ReadUInt16();

                ushort globalInfoLength = reader.ReadUInt16();
                byte[] globalInfoBytes = reader.ReadBytes(globalInfoLength);
                GlobalInfo = new GlobalInfo(globalInfoBytes);

                ushort availableWorldsCount = reader.ReadUInt16();
                ActiveWorlds = new Dictionary<string, WorldDetails>();
                for (int i = 0; i < availableWorldsCount; i++)
                {
                    string key = reader.ReadString();
                    int worldDetailsLength = reader.ReadUInt16();
                    byte[] worldDetailsBytes = reader.ReadBytes(worldDetailsLength);
                    WorldDetails worldDetails = new WorldDetails(worldDetailsBytes);
                    ActiveWorlds[key] = worldDetails;
                }

                ushort worldBuildsFTPNetworkSettingsLength = reader.ReadUInt16();
                byte[] worldsStoreFTPNetworkSettingsBytes = reader.ReadBytes(worldBuildsFTPNetworkSettingsLength);
                WorldBuildsFTPServerSettings = new ServerConnectionSettings(worldsStoreFTPNetworkSettingsBytes);

                ushort defaultWorldSubStoreFTPServerSettingsLength = reader.ReadUInt16();
                byte[] defaultWorldSubStoreFTPServerSettingsBytes = reader.ReadBytes(defaultWorldSubStoreFTPServerSettingsLength);
                DefaultWorldSubStoreFTPServerSettings = new ServerConnectionSettings(defaultWorldSubStoreFTPServerSettingsBytes);

                ushort defaultInstanceServerSettingsLength = reader.ReadUInt16();
                byte[] defaultInstanceServerSettingsBytes = reader.ReadBytes(defaultInstanceServerSettingsLength);
                DefaultInstanceServerSettings = new ServerConnectionSettings(defaultInstanceServerSettingsBytes);
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


        internal class PlatformInstanceInfo : InstanceInfoBase
        {
            public Dictionary<ushort, PlatformClientInfo> ClientInfos { get; private set; }

            public PlatformInstanceInfo()
            {
                ClientInfos = new Dictionary<ushort, PlatformClientInfo>();
            }

            public PlatformInstanceInfo(byte[] bytes) : base(bytes) { }

            public PlatformInstanceInfo(string worldName, string instanceSuffix, string versionNumber, Dictionary<ushort, PlatformClientInfo> clientInfos) : base(worldName, instanceSuffix, versionNumber)
            {
                ClientInfos = clientInfos;
            }

            public PlatformInstanceInfo(string fullInstanceCode, Dictionary<ushort, PlatformClientInfo> clientInfos) : base(fullInstanceCode)
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


        internal class InstanceAllocationRequest : VE2Serializable
        {
            public string WorldName { get; private set; }
            public string InstanceSuffix { get; private set; }
            public string VersionNumber { get; private set; }
            public string FullInstanceCode => $"{WorldName}-{InstanceSuffix}-{VersionNumber}";

            public InstanceAllocationRequest(byte[] bytes) : base(bytes) { }

            public InstanceAllocationRequest(string worldName, string instanceSuffix, string versionNumber)
            {
                WorldName = worldName;
                InstanceSuffix = instanceSuffix;
                VersionNumber = versionNumber;
            }

            protected override byte[] ConvertToBytes()
            {
                using MemoryStream stream = new();
                using BinaryWriter writer = new(stream);

                writer.Write(WorldName);
                writer.Write(InstanceSuffix);
                writer.Write(VersionNumber);

                return stream.ToArray();
            }

            protected override void PopulateFromBytes(byte[] data)
            {
                using MemoryStream stream = new(data);
                using BinaryReader reader = new(stream);

                WorldName = reader.ReadString();
                InstanceSuffix = reader.ReadString();
                VersionNumber = reader.ReadString();
            }

        }
    }
}
