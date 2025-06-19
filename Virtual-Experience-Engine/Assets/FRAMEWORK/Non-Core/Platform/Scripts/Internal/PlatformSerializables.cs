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
        internal static readonly int PlatformNetcodeVersion = 2;

        public enum PlatformNetworkingMessageCodes
        {
            NetcodeVersionConfirmation,
            ServerRegistrationRequest,
            ServerRegistrationResponse,
            GlobalInfo,
            InstanceAllocationRequest,
            UpdatePlayerPresentation,
            AdminUpdateNotice
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
            public InstanceCode StartingInstanceCode { get; private set; }
            public PlayerPresentationConfig PlayerPresentationConfig;

            public ServerRegistrationRequest(string customerID, string customerKey, InstanceCode startingInstanceCode, PlayerPresentationConfig playerPresentationConfig)
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

                writer.Write(StartingInstanceCode.ToString());

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

                StartingInstanceCode = new InstanceCode(reader.ReadString());

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


        internal class GlobalInfo : VE2Serializable
        {
            public Dictionary<InstanceCode, PlatformInstanceInfo> InstanceInfos { get; private set; }

            public GlobalInfo(byte[] bytes) : base(bytes) { }

            public GlobalInfo(Dictionary<InstanceCode, PlatformInstanceInfo> instanceInfos)
            {
                InstanceInfos = instanceInfos;
            }

            public GlobalInfo()
            {
                InstanceInfos = new Dictionary<InstanceCode, PlatformInstanceInfo>();
            }

            protected override byte[] ConvertToBytes()
            {
                using MemoryStream stream = new();
                using BinaryWriter writer = new(stream);

                writer.Write((ushort)InstanceInfos.Count);

                foreach (var kvp in InstanceInfos)
                {
                    writer.Write(kvp.Key.ToString());
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
                InstanceInfos = new Dictionary<InstanceCode, PlatformInstanceInfo>();

                for (int i = 0; i < instanceInfoCount; i++)
                {
                    InstanceCode instanceCode = new(reader.ReadString());
                    ushort instanceInfoBytesLength = reader.ReadUInt16();
                    byte[] instanceInfoBytes = reader.ReadBytes(instanceInfoBytesLength);
                    PlatformInstanceInfo instanceInfo = new(instanceInfoBytes);
                    InstanceInfos[instanceCode] = instanceInfo;
                }
            }

        }


        internal class InstanceAllocationRequest : VE2Serializable
        {
            public InstanceCode InstanceCode { get; private set; }

            public InstanceAllocationRequest(byte[] bytes) : base(bytes) { }

            public InstanceAllocationRequest(InstanceCode instanceCode)
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

            protected override void PopulateFromBytes(byte[] data)
            {
                using MemoryStream stream = new(data);
                using BinaryReader reader = new(stream);

                InstanceCode = new InstanceCode(reader.ReadString());
            }
        }

        public class AdminUpdateNotice : VE2Serializable
        {
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

                writer.Write(IsAdmin);
                return stream.ToArray();
            }

            protected override void PopulateFromBytes(byte[] bytes)
            {
                using MemoryStream stream = new(bytes);
                using BinaryReader reader = new(stream);

                IsAdmin = reader.ReadBoolean();
            }
        }
    }
}
