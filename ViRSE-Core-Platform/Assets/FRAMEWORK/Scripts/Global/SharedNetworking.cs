using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using ViRSE;

namespace ViRSE
{
    public static class NetworkUtils
    {
        public static readonly int NetcodeVersion = 5;

        public enum MessageCodes
        {
            NetcodeVersionConfirmation,
            ServerRegistrationRequest,
            ServerRegistrationConfirmation,
            PopulationInfo,
            UpdatePlayerSettingsRequest,
            WorldstateSyncableBundle,
        }
    }


    public abstract class ViRSENetworkSerializable
    {
        public byte[] Bytes { get => ConvertToBytes(); set => PopulateFromBytes(value); }

        public ViRSENetworkSerializable() { }

        public ViRSENetworkSerializable(byte[] bytes)
        {
            PopulateFromBytes(bytes);
        }

        protected abstract byte[] ConvertToBytes();

        protected abstract void PopulateFromBytes(byte[] bytes);
    }


    public class NetcodeVersionConfirmation : ViRSENetworkSerializable
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


    public class ServerRegistrationRequest : ViRSENetworkSerializable
    {
        public string StartingInstance { get; private set; }
        public string MachineName { get; private set; }
        public UserIdentity UserIdentity { get; private set; }

        public ServerRegistrationRequest(string startingInstance, string machineName, UserIdentity userIdentity)
        {
            StartingInstance = startingInstance;
            MachineName = machineName;
            UserIdentity = userIdentity;
        }

        public ServerRegistrationRequest(byte[] bytes) : base(bytes) { }

        protected override byte[] ConvertToBytes()
        {
            using MemoryStream stream = new();
            using BinaryWriter writer = new(stream);

            writer.Write(StartingInstance);
            writer.Write(MachineName);

            bool hasUserIdentity = UserIdentity != null;
            writer.Write(hasUserIdentity);
            if (hasUserIdentity)
            {
                writer.Write((ushort)UserIdentity.Bytes.Length);
                writer.Write(UserIdentity.Bytes);
            }

            return stream.ToArray();
        }

        protected override void PopulateFromBytes(byte[] bytes)
        {
            using MemoryStream stream = new(bytes);
            using BinaryReader reader = new(stream);

            StartingInstance = reader.ReadString();
            MachineName = reader.ReadString();

            bool hasUserIdentity = reader.ReadBoolean();

            if (hasUserIdentity)
            {
                int userIdentBytesLength = reader.ReadUInt16();
                UserIdentity = new(reader.ReadBytes(userIdentBytesLength));
            }
            else
            {
                UserIdentity = null;
            }
        }
    }


    [Serializable]
    public class UserIdentity : ViRSENetworkSerializable
    {
        public string samAccountName;
        public string firstName;
        public string lastName;
        public string jobTitle;
        public string faculty;
        public string department;
        public string email;

        public UserIdentity(byte[] bytes) : base(bytes) { }

        public UserIdentity(string samAccountName, string firstName, string lastName, string jobTitle, string faculty, string department, string email)
        {
            this.samAccountName = samAccountName;
            this.firstName = firstName;
            this.lastName = lastName;
            this.jobTitle = jobTitle;
            this.faculty = faculty;
            this.department = department;
            this.email = email;
        }

        protected override byte[] ConvertToBytes()
        {
            using MemoryStream stream = new();
            using BinaryWriter writer = new(stream);

            writer.Write(samAccountName);
            writer.Write(firstName);
            writer.Write(lastName);
            writer.Write(jobTitle);
            writer.Write(faculty);
            writer.Write(department);
            writer.Write(email);

            return stream.ToArray();
        }

        protected override void PopulateFromBytes(byte[] bytes)
        {
            using MemoryStream stream = new(bytes);
            using BinaryReader reader = new(stream);

            samAccountName = reader.ReadString();
            firstName = reader.ReadString();
            lastName = reader.ReadString();
            jobTitle = reader.ReadString();
            faculty = reader.ReadString();
            department = reader.ReadString();
            email = reader.ReadString();
        }
    }


    public class ServerRegistrationConfirmation : ViRSENetworkSerializable
    {
        public ushort LocalPlayerID { get; private set; }
        public bool CompletedTutorial { get; private set; }
        /// <summary>
        /// Will be null if this user has never saves user settings
        /// </summary>
        public UserSettings UserSettings { get; private set; }
        public PopulationInfo PopulationInfo { get; private set; }


        public ServerRegistrationConfirmation(byte[] bytes) : base(bytes) { }

        public ServerRegistrationConfirmation(ushort localPlayerID, bool completedTutorial, UserSettings userSettings, PopulationInfo populationInfo)
        {
            LocalPlayerID = localPlayerID;
            CompletedTutorial = completedTutorial;
            UserSettings = userSettings;
            PopulationInfo = populationInfo;
        }

        protected override byte[] ConvertToBytes()
        {
            using MemoryStream stream = new();
            using BinaryWriter writer = new(stream);

            writer.Write(LocalPlayerID);
            writer.Write(CompletedTutorial);

            bool UserSettingsPresent = UserSettings != null;
            writer.Write(UserSettingsPresent);

            if (UserSettingsPresent)
            {
                writer.Write((ushort)UserSettings.Bytes.Length);
                writer.Write(UserSettings.Bytes);
            }

            writer.Write(PopulationInfo.Bytes.Length); //Keeping this as a full Int32
            writer.Write(PopulationInfo.Bytes);

            return stream.ToArray();
        }

        protected override void PopulateFromBytes(byte[] bytes)
        {
            using MemoryStream stream = new(bytes);
            using BinaryReader reader = new(stream);

            LocalPlayerID = reader.ReadUInt16();
            CompletedTutorial = reader.ReadBoolean();

            bool UserSettingsPresent = reader.ReadBoolean();

            if (UserSettingsPresent)
            {
                int userSettingsBytesLength = reader.ReadUInt16();
                UserSettings = new(reader.ReadBytes(userSettingsBytesLength));
            }
            else
            {
                UserSettings = null;
            }

            int populationInfoBytesLength = reader.ReadInt32();
            PopulationInfo = new(reader.ReadBytes(populationInfoBytesLength));

        }
    }


    public class UserSettings : ViRSENetworkSerializable
    {
        public string DisplayName;
        public float MasterVolume;
        public float GameVolume;
        public float ChatVolume;
        public ushort HeadType;
        public ushort TorsoType;
        public ushort HandsType;
        public float ColorRed;
        public float ColorGreen;
        public float ColorBlue;
        public float LookSensitivity;
        public bool HoldToCrouch;
        public float DragSpeed;
        public bool DragDarkening;
        public bool TeleportDarkening;
        public bool SnapTurnDarkening;
        public float SnapTurnAmount;
        public bool Vibrate;
        public bool ToolCycling;
        public bool ControllerLabels;
        public float WristLookPrecision;

        public UserSettings(byte[] bytes) : base(bytes) { }

        private UserSettings(string displayName, float masterVolume, float gameVolume, float chatVolume, ushort headType, ushort torsoType, ushort handsType, float colorRed, float colorGreen, float colorBlue, float lookSensitivity, bool holdToCrouch, float dragSpeed, bool dragDarkening, bool teleportDarkening, bool snapTurnDarkening, float snapTurnAmount, bool vibrate, bool toolCycling, bool controllerLabels, float wristLookPrecision)
        {
            DisplayName = displayName;
            MasterVolume = masterVolume;
            GameVolume = gameVolume;
            ChatVolume = chatVolume;
            HeadType = headType;
            TorsoType = torsoType;
            HandsType = handsType;
            ColorRed = colorRed;
            ColorGreen = colorGreen;
            ColorBlue = colorBlue;
            LookSensitivity = lookSensitivity;
            HoldToCrouch = holdToCrouch;
            DragSpeed = dragSpeed;
            DragDarkening = dragDarkening;
            TeleportDarkening = teleportDarkening;
            SnapTurnDarkening = snapTurnDarkening;
            SnapTurnAmount = snapTurnAmount;
            Vibrate = vibrate;
            ToolCycling = toolCycling;
            ControllerLabels = controllerLabels;
            WristLookPrecision = wristLookPrecision;
        }

        public static UserSettings GenerateDefaults()
        {
            return new UserSettings(
                displayName: "Guest",
                masterVolume: 1,
                gameVolume: 1,
                chatVolume: 1,
                headType: 0,
                torsoType: 0,
                handsType: 0,
                colorRed: 1,
                colorGreen: 0.6f,
                colorBlue: 0.5f,
                lookSensitivity: 1,
                holdToCrouch: true,
                dragSpeed: 5,
                dragDarkening: false,
                teleportDarkening: false,
                snapTurnDarkening: false,
                snapTurnAmount: 22.5f,
                vibrate: true,
                toolCycling: false,
                controllerLabels: true,
                wristLookPrecision: 2);
        }

        protected override byte[] ConvertToBytes()
        {
            using MemoryStream stream = new();
            using BinaryWriter writer = new(stream);

            writer.Write(DisplayName);
            writer.Write(MasterVolume);
            writer.Write(GameVolume);
            writer.Write(ChatVolume);
            writer.Write(HeadType);
            writer.Write(TorsoType);
            writer.Write(HandsType);
            writer.Write(ColorRed);
            writer.Write(ColorGreen);
            writer.Write(ColorBlue);
            writer.Write(LookSensitivity);
            writer.Write(HoldToCrouch);
            writer.Write(DragSpeed);
            writer.Write(DragDarkening);
            writer.Write(TeleportDarkening);
            writer.Write(SnapTurnDarkening);
            writer.Write(SnapTurnAmount);
            writer.Write(Vibrate);
            writer.Write(ToolCycling);
            writer.Write(ControllerLabels);
            writer.Write(WristLookPrecision);

            return stream.ToArray();
        }

        protected override void PopulateFromBytes(byte[] data)
        {
            using MemoryStream stream = new(data);
            using BinaryReader reader = new(stream);

            DisplayName = reader.ReadString();
            MasterVolume = reader.ReadSingle();
            GameVolume = reader.ReadSingle();
            ChatVolume = reader.ReadSingle();
            HeadType = reader.ReadUInt16();
            TorsoType = reader.ReadUInt16();
            HandsType = reader.ReadUInt16();
            ColorRed = reader.ReadSingle();
            ColorGreen = reader.ReadSingle();
            ColorBlue = reader.ReadSingle();
            LookSensitivity = reader.ReadSingle();
            HoldToCrouch = reader.ReadBoolean();
            DragSpeed = reader.ReadSingle();
            DragDarkening = reader.ReadBoolean();
            TeleportDarkening = reader.ReadBoolean();
            SnapTurnDarkening = reader.ReadBoolean();
            SnapTurnAmount = reader.ReadSingle();
            Vibrate = reader.ReadBoolean();
            ToolCycling = reader.ReadBoolean();
            ControllerLabels = reader.ReadBoolean();
            WristLookPrecision = reader.ReadSingle();
        }
    }


    public class PopulationInfo : ViRSENetworkSerializable
    {
        public Dictionary<string, InstanceInfo> InstanceInfos = new();

        public PopulationInfo(Dictionary<string, InstanceInfo> instanceInfos)
        {
            this.InstanceInfos = instanceInfos;
        }

        public PopulationInfo() { }

        public PopulationInfo(byte[] bytes) : base(bytes) { }

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
                string key = reader.ReadString();
                int instanceInfoBytesLength = reader.ReadUInt16();
                byte[] instanceInfoBytes = reader.ReadBytes(instanceInfoBytesLength);
                InstanceInfo instanceInfo = new InstanceInfo(instanceInfoBytes);
                InstanceInfos[key] = instanceInfo;
            }
        }
    }


    public class InstanceInfo : ViRSENetworkSerializable
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


    public class ClientInfo : ViRSENetworkSerializable
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


    public class AvatarDetails : ViRSENetworkSerializable
    {
        public ushort headFrameworkAvatarType;
        public ushort torsoFrameworkAvatarType;
        public float frameworkColourRed;
        public float frameworkColourGreen;
        public float frameworkColourBlue;

        //TODO, might want to rethink this 
        /*The plugin overrides are really part of the instance, so it should be handled by PlayerSyncer... should be part of PlayerState!!!!
         * Then, we can put the AvatarDetails within the UserSettings object 
         * Nobody outside the instance (i.e, not being sycned by player syncer) needs avatar overrides anwyay
         */

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


    public class InstanceAllocationRequest : ViRSENetworkSerializable
    {
        public string InstanceCode { get; private set; }

        public InstanceAllocationRequest(string instanceCode)
        {
            InstanceCode = instanceCode;
        }

        public InstanceAllocationRequest(byte[] bytes) : base(bytes) { }

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



