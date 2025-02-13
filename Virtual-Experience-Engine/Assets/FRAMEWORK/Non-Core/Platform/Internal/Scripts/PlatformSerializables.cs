using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using static NonCoreCommonSerializables;
using static VE2.Common.CommonSerializables;
using static VE2.Platform.API.PlatformPublicSerializables;



#if UNITY_EDITOR
using UnityEngine;
#endif

namespace VE2.Platform.Internal
{
    public class PlatformSerializables
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
                DefaultInstanceServerSettings = new ServerConnectionSettings(defaultWorldSubStoreFTPServerSettingsBytes);
            }
        }

        internal class InstanceAllocationRequest : VE2Serializable
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
