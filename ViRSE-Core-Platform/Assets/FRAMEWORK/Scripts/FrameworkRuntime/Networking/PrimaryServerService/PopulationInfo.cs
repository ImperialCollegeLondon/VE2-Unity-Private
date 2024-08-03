using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace ViRSE.FrameworkRuntime
{
    public class PopulationInfo : VSerializable
    {
        public string LocalClientID { get; }
        public string LocalClientInstanceCode { get; }

        public Dictionary<string, InstanceInfo> InstanceInfos = new();

        public ClientInfo LocalClientInfo => LocalInstanceInfo.ClientInfos[LocalClientID];
        public InstanceInfo LocalInstanceInfo => InstanceInfos[LocalClientInstanceCode];

        public PopulationInfo(string localClientID, string localClientInstanceCode, Dictionary<string, InstanceInfo> instanceInfos)
        {
            this.LocalClientID = localClientID;
            this.LocalClientInstanceCode = localClientInstanceCode;
            this.InstanceInfos = instanceInfos;
        }

        protected override byte[] ConvertToBytes()
        {
            using MemoryStream stream = new();
            using BinaryWriter writer = new(stream);

            writer.Write(InstanceInfos.Count);
            foreach (var kvp in InstanceInfos)
            {
                writer.Write(kvp.Key);
                byte[] instanceInfoBytes = kvp.Value.Bytes;
                writer.Write(instanceInfoBytes.Length);
                writer.Write(instanceInfoBytes);
            }

            return stream.ToArray();
        }


        protected override void PopulateFromBytes(byte[] data)
        {
            using MemoryStream stream = new(data);
            using BinaryReader reader = new(stream);

            int instanceInfoCount = reader.ReadInt32();
            InstanceInfos = new Dictionary<string, InstanceInfo>(instanceInfoCount);
            for (int i = 0; i < instanceInfoCount; i++)
            {
                string key = reader.ReadString();
                int instanceInfoBytesLength = reader.ReadInt32();
                byte[] instanceInfoBytes = reader.ReadBytes(instanceInfoBytesLength);
                InstanceInfo instanceInfo = new InstanceInfo(instanceInfoBytes);
                InstanceInfos[key] = instanceInfo;
            }
        }
    }

    public class InstanceInfo : VSerializable
    {
        public string InstanceCode;
        public string HostID;
        public bool InstanceMuted; //TODO should this live here?
        public Dictionary<string, ClientInfo> ClientInfos;

        public InstanceInfo(string instanceCode, string hostID, bool instanceMuted, Dictionary<string, ClientInfo> clientInfos)
        {
            this.InstanceCode = instanceCode;
            this.HostID = hostID;
            this.InstanceMuted = instanceMuted;
            this.ClientInfos = clientInfos;
        }

        public InstanceInfo(byte[] bytes) : base(bytes) { }

        protected override byte[] ConvertToBytes()
        {
            using MemoryStream stream = new();
            using BinaryWriter writer = new(stream);

            writer.Write(InstanceCode);
            writer.Write(HostID);
            writer.Write(InstanceMuted);

            writer.Write(ClientInfos.Count);
            foreach (var kvp in ClientInfos)
            {
                writer.Write(kvp.Key);
                byte[] clientInfoBytes = kvp.Value.Bytes;
                writer.Write(clientInfoBytes.Length);
                writer.Write(clientInfoBytes);
            }

            return stream.ToArray();
        }

        protected override void PopulateFromBytes(byte[] data)
        {
            using MemoryStream stream = new(data);
            using BinaryReader reader = new(stream);

            InstanceCode = reader.ReadString();
            HostID = reader.ReadString();
            InstanceMuted = reader.ReadBoolean();

            int clientInfoCount = reader.ReadInt32();
            ClientInfos = new Dictionary<string, ClientInfo>(clientInfoCount);
            for (int i = 0; i < clientInfoCount; i++)
            {
                string key = reader.ReadString();
                int clientInfoBytesLength = reader.ReadInt32();
                byte[] clientInfoBytes = reader.ReadBytes(clientInfoBytesLength);
                ClientInfo clientInfo = new ClientInfo(clientInfoBytes);
                ClientInfos[key] = clientInfo;
            }
        }
    }

    public class ClientInfo : VSerializable
    {
        public ushort ClientID;
        public string DisplayName;
        public bool IsAdmin;
        public string MachineName;

        public AvatarDetails AvatarDetails;

        public ClientInfo(ushort clientID, string displayName, bool isAdmin, string machineName, AvatarDetails avatarDetails)
        {
            this.ClientID = clientID;
            this.DisplayName = displayName;
            this.IsAdmin = isAdmin;
            this.MachineName = machineName;
            this.AvatarDetails = avatarDetails;
        }

        public ClientInfo(byte[] bytes) : base(bytes) { }

        protected override byte[] ConvertToBytes()
        {
            using MemoryStream stream = new();
            using BinaryWriter writer = new(stream);

            writer.Write(ClientID);
            writer.Write(DisplayName);
            writer.Write(IsAdmin);
            writer.Write(MachineName);

            byte[] avatarDetailsBytes = AvatarDetails.Bytes;
            writer.Write(avatarDetailsBytes.Length);
            writer.Write(avatarDetailsBytes);

            return stream.ToArray();
        }

        protected override void PopulateFromBytes(byte[] bytes)
        {
            using MemoryStream stream = new(bytes);
            using BinaryReader reader = new(stream);

            ClientID = reader.ReadUInt16();
            DisplayName = reader.ReadString();
            IsAdmin = reader.ReadBoolean();
            MachineName = reader.ReadString();

            int avatarDetailsLength = reader.ReadInt32();
            byte[] avatarDetailsBytes = reader.ReadBytes(avatarDetailsLength);
            AvatarDetails = new (avatarDetailsBytes);
        }
    }

    public class AvatarDetails : VSerializable
    {
        public ushort headFrameworkAvatarType;
        public ushort torsoFrameworkAvatarType;
        public float frameworkColourRed;
        public float frameworkColourGreen;
        public float frameworkColourBlue;

        public ushort headPluginAvatarType;
        public ushort torsoPluginAvatarType;
        public ushort handsPluginAvatarType;

        public bool showAvatar;

        public AvatarDetails(byte[] bytes) : base(bytes) { }

        public AvatarDetails()
        {
            headFrameworkAvatarType = 0;
            torsoFrameworkAvatarType = 0;
            headPluginAvatarType = 0;
            torsoPluginAvatarType = 0;
            handsPluginAvatarType = 0;
            frameworkColourRed = 0.5f;
            frameworkColourGreen = 0.5f;
            frameworkColourBlue = 0.5f;
            showAvatar = true;
        }

        protected override byte[] ConvertToBytes()
        {
            using MemoryStream stream = new();
            using BinaryWriter writer = new(stream);

            writer.Write(headFrameworkAvatarType);
            writer.Write(torsoFrameworkAvatarType);
            writer.Write(frameworkColourRed);
            writer.Write(frameworkColourGreen);
            writer.Write(frameworkColourBlue);
            writer.Write(headPluginAvatarType);
            writer.Write(torsoPluginAvatarType);
            writer.Write(handsPluginAvatarType);
            writer.Write(showAvatar);

            return stream.ToArray();
        }

        protected override void PopulateFromBytes(byte[] data)
        {
            using MemoryStream stream = new(data);
            using BinaryReader reader = new(stream);

            headFrameworkAvatarType = reader.ReadUInt16();
            torsoFrameworkAvatarType = reader.ReadUInt16();
            frameworkColourRed = reader.ReadSingle();
            frameworkColourGreen = reader.ReadSingle();
            frameworkColourBlue = reader.ReadSingle();
            headPluginAvatarType = reader.ReadUInt16();
            torsoPluginAvatarType = reader.ReadUInt16();
            handsPluginAvatarType = reader.ReadUInt16();
            showAvatar = reader.ReadBoolean();
        }
    }
}
