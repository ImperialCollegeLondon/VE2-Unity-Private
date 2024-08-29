using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using ViRSE.Core.Shared;

public class CommonNetworkObjects
{
    public class NetcodeVersionConfirmation : ViRSESerializable
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

    public class InstanceInfo : ViRSESerializable
    {
        public string InstanceCode;
        public ushort HostID;
        public bool InstanceMuted; //TODO should this live here?
        public Dictionary<ushort, ClientInfo> ClientInfos;

        public InstanceInfo(string instanceCode, ushort hostID, bool instanceMuted, Dictionary<ushort, ClientInfo> clientInfos)
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

            InstanceCode = reader.ReadString();
            HostID = reader.ReadUInt16();
            InstanceMuted = reader.ReadBoolean();

            int clientInfoCount = reader.ReadUInt16();
            ClientInfos = new Dictionary<ushort, ClientInfo>();
            for (int i = 0; i < clientInfoCount; i++)
            {
                ushort key = reader.ReadUInt16();
                int clientInfoBytesLength = reader.ReadUInt16();
                byte[] clientInfoBytes = reader.ReadBytes(clientInfoBytesLength);
                ClientInfo clientInfo = new ClientInfo(clientInfoBytes);
                ClientInfos[key] = clientInfo;
            }
        }
    }


    public class ClientInfo : ViRSESerializable
    {
        public ushort ClientID;
        public string DisplayName;
        public bool IsAdmin;
        public string MachineName;

        public AvatarAppearance AvatarDetails;

        public ClientInfo(ushort clientID, string displayName, bool isAdmin, string machineName, AvatarAppearance avatarDetails)
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

            bool hasAvatarDetails = AvatarDetails != null;
            writer.Write(hasAvatarDetails);

            if (hasAvatarDetails)
            {
                byte[] avatarDetailsBytes = AvatarDetails.Bytes;
                writer.Write((ushort)avatarDetailsBytes.Length);
                writer.Write(avatarDetailsBytes);
            }

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

            bool hasAvatarDetails = reader.ReadBoolean();
            if (hasAvatarDetails)
            {
                int avatarDetailsLength = reader.ReadUInt16();
                byte[] avatarDetailsBytes = reader.ReadBytes(avatarDetailsLength);
                AvatarDetails = new(avatarDetailsBytes);
            }
        }
    }

    public class AvatarAppearance : ViRSESerializable
    {
        public bool UsingViRSEAvatar;
        public string PlayerName; //TODO - maybe name shouldn't live here? Still want name, even if not using the default rig?
        public string AvatarHeadType;
        public string AvatarBodyType;
        public float AvatarRed;
        public float AvatarGreen;
        public float AvatarBlue;
        public bool showAvatar = true;

        public AvatarAppearance(byte[] bytes) : base(bytes) { }

        public AvatarAppearance(bool usingViRSEAvatar, string playerName, string avatarHeadType, string avatarBodyType, float avatarRed, float avatarGreen, float avatarBlue)
        {
            UsingViRSEAvatar = usingViRSEAvatar;
            PlayerName = playerName;
            AvatarHeadType = avatarHeadType;
            AvatarBodyType = avatarBodyType;
            AvatarRed = avatarRed;
            AvatarGreen = avatarGreen;
            AvatarBlue = avatarBlue;
        }

        protected override byte[] ConvertToBytes()
        {
            using MemoryStream stream = new();
            using BinaryWriter writer = new(stream);

            writer.Write(UsingViRSEAvatar);
            writer.Write(PlayerName);
            writer.Write(AvatarHeadType);
            writer.Write(AvatarBodyType);
            writer.Write(AvatarRed);
            writer.Write(AvatarGreen);
            writer.Write(AvatarBlue);
            writer.Write(showAvatar);
            writer.Write(showAvatar);

            return stream.ToArray();
        }

        protected override void PopulateFromBytes(byte[] data)
        {
            using MemoryStream stream = new(data);
            using BinaryReader reader = new(stream);

            UsingViRSEAvatar = reader.ReadBoolean();
            PlayerName = reader.ReadString();
            AvatarHeadType = reader.ReadString();
            AvatarBodyType = reader.ReadString();
            AvatarRed = reader.ReadSingle();
            AvatarGreen = reader.ReadSingle();
            AvatarBlue = reader.ReadSingle();
            showAvatar = reader.ReadBoolean();
        }
    }

    //public class AvatarDetails : ViRSENetworkSerializable
    //{
    //    public ushort headFrameworkAvatarType;
    //    public ushort torsoFrameworkAvatarType;
    //    public float frameworkColourRed;
    //    public float frameworkColourGreen;
    //    public float frameworkColourBlue;

    //    //TODO, might want to rethink this 
    //    /*The plugin overrides are really part of the instance, so it should be handled by PlayerSyncer... should be part of PlayerState!!!!
    //     * Then, we can put the AvatarDetails within the UserSettings object 
    //     * Nobody outside the instance (i.e, not being sycned by player syncer) needs avatar overrides anwyay
    //     */

    //    public ushort headPluginAvatarType;
    //    public ushort torsoPluginAvatarType;
    //    public ushort handsPluginAvatarType;

    //    public bool showAvatar;

    //    public AvatarDetails(byte[] bytes) : base(bytes) { }

    //    public AvatarDetails()
    //    {
    //        headFrameworkAvatarType = 0;
    //        torsoFrameworkAvatarType = 0;
    //        headPluginAvatarType = 0;
    //        torsoPluginAvatarType = 0;
    //        handsPluginAvatarType = 0;
    //        frameworkColourRed = 0.5f;
    //        frameworkColourGreen = 0.5f;
    //        frameworkColourBlue = 0.5f;
    //        showAvatar = true;
    //    }

    //    protected override byte[] ConvertToBytes()
    //    {
    //        using MemoryStream stream = new();
    //        using BinaryWriter writer = new(stream);

    //        writer.Write(headFrameworkAvatarType);
    //        writer.Write(torsoFrameworkAvatarType);
    //        writer.Write(frameworkColourRed);
    //        writer.Write(frameworkColourGreen);
    //        writer.Write(frameworkColourBlue);
    //        writer.Write(headPluginAvatarType);
    //        writer.Write(torsoPluginAvatarType);
    //        writer.Write(handsPluginAvatarType);
    //        writer.Write(showAvatar);

    //        return stream.ToArray();
    //    }

    //    protected override void PopulateFromBytes(byte[] data)
    //    {
    //        using MemoryStream stream = new(data);
    //        using BinaryReader reader = new(stream);

    //        headFrameworkAvatarType = reader.ReadUInt16();
    //        torsoFrameworkAvatarType = reader.ReadUInt16();
    //        frameworkColourRed = reader.ReadSingle();
    //        frameworkColourGreen = reader.ReadSingle();
    //        frameworkColourBlue = reader.ReadSingle();
    //        headPluginAvatarType = reader.ReadUInt16();
    //        torsoPluginAvatarType = reader.ReadUInt16();
    //        handsPluginAvatarType = reader.ReadUInt16();
    //        showAvatar = reader.ReadBoolean();
    //    }
    //}
}
