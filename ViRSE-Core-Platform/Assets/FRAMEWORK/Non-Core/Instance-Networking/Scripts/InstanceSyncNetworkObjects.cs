using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using ViRSE.Core.Shared;
using static CommonNetworkObjects;

public class InstanceSyncNetworkObjects
{
    public static readonly int InstanceNetcodeVersion = 5;

    public enum InstanceNetworkingMessageCodes
    {
        NetcodeVersionConfirmation,
        ServerRegistrationRequest,
        ServerRegistrationConfirmation,
        PopulationInfo,
        UpdatePlayerSettingsRequest,
        WorldstateSyncableBundle,
    }

    //So what actually is this registration request?
    //Needs to have the avatar presentation details
    //Also needs the instance code 

    public class ServerRegistrationRequest : ViRSESerializable
    {
        public string InstanceCode { get; private set; }
        public AvatarDetails AvatarDetails { get; private set; }

        public ServerRegistrationRequest(byte[] bytes) : base(bytes) { }

        public ServerRegistrationRequest(AvatarDetails avatarDetails, string instanceCode)
        {
            AvatarDetails = avatarDetails;
            InstanceCode = instanceCode;
        }

        protected override byte[] ConvertToBytes()
        {
            using MemoryStream stream = new();
            using BinaryWriter writer = new(stream);

            writer.Write(InstanceCode);
            writer.Write((ushort)AvatarDetails.Bytes.Length);
            writer.Write(AvatarDetails.Bytes);

            return stream.ToArray();
        }

        protected override void PopulateFromBytes(byte[] bytes)
        {
            using MemoryStream stream = new(bytes);
            using BinaryReader reader = new(stream);

            InstanceCode = reader.ReadString(); 
            ushort avatarDetailsLength = reader.ReadUInt16();
            AvatarDetails = new AvatarDetails(reader.ReadBytes(avatarDetailsLength));
        }
    }

    public class ServerRegistrationConfirmation : ViRSESerializable
    {
        public ushort LocalClientID { get; private set; }
        public InstanceInfo InstanceInfo { get; private set; }

        public ServerRegistrationConfirmation(byte[] bytes) : base(bytes) { }

        public ServerRegistrationConfirmation(ushort localClientID, InstanceInfo instanceInfo)
        {
            LocalClientID = localClientID;
            InstanceInfo = instanceInfo;
        }

        protected override byte[] ConvertToBytes()
        {
            using MemoryStream stream = new();
            using BinaryWriter writer = new(stream);

            writer.Write(LocalClientID);
            writer.Write((ushort)InstanceInfo.Bytes.Length);
            writer.Write(InstanceInfo.Bytes);

            return stream.ToArray();
        }

        protected override void PopulateFromBytes(byte[] bytes)
        {
            using MemoryStream stream = new(bytes);
            using BinaryReader reader = new(stream);

            LocalClientID = reader.ReadUInt16();
            ushort instanceInfoLength = reader.ReadUInt16();
            InstanceInfo = new InstanceInfo(reader.ReadBytes(instanceInfoLength));
        }
    }
}
