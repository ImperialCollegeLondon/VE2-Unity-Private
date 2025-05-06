using System;
using System.IO;
using UnityEngine;
using UnityEngine.Events;
using VE2.Common.Shared;
using VE2.Core.VComponents.Shared;
using VE2.NonCore.Instancing.API;
using static VE2.Common.Shared.CommonSerializables;

namespace VE2.NonCore.Instancing.Internal
{
    [Serializable]
    internal class RigidbodySyncableStateConfig : BaseWorldStateConfig {

        public bool LogSendReceiveDebugMessages = false;
        public bool LogInterpolationDebug = false;
        public bool DrawInterpolationLines = false;
    }

    internal class RigidbodySyncableStateModule : BaseWorldStateModule, IRigidbodySyncableStateModule
    {
        /// <value>
        /// <see cref="float"/>: Fixed time sent, <see cref="Vector3"/>: Position, <see cref="Quaternion"/>: Rotation
        /// </value>
        public UnityEvent<RigidbodySyncableState> OnReceiveState = new();
        private RigidbodySyncableState _state => (RigidbodySyncableState)State;
        private RigidbodySyncableStateConfig _config => (RigidbodySyncableStateConfig)Config;

        public RigidbodySyncableStateModule(VE2Serializable state, BaseWorldStateConfig config, string id, IWorldStateSyncableContainer worldStateSyncableContainer) :
            base(state, config, id, worldStateSyncableContainer)  { }

        protected override void UpdateBytes(byte[] newBytes)
        {
            _state.Bytes = newBytes;
            try
            {
                OnReceiveState?.Invoke(_state);
            }
            catch (Exception e)
            {
                Debug.Log($"Error when emitting OnReceiveState from RigidbodySyncable with ID {ID} \n{e.Message}\n{e.StackTrace}");
            }
        }

        public void SetStateFromHost(float fixedTime, Vector3 position, Quaternion rotation, uint grabID)
        {
            _state.FromHost = true;
            _state.FixedTime = fixedTime;
            _state.Position = position;
            _state.Rotation = rotation;
            _state.GrabCounter = grabID;
        }

        public void SetStateFromNonHost(float fixedTime, Vector3 position, Quaternion rotation, uint grabID, float latestPing, Vector3 velocity, Vector3 angularVelocity)
        {
            _state.FromHost = false;
            _state.FixedTime = fixedTime;
            _state.Position = position;
            _state.Rotation = rotation;
            _state.GrabCounter = grabID;
            _state.LatestRoundTripTime = latestPing;
            _state.Velocity = velocity;
            _state.AngularVelocity = angularVelocity;
        }
    }

    [Serializable]
    internal class RigidbodySyncableState : VE2Serializable
    {
        public bool FromHost;

        public float FixedTime;
        public Vector3 Position { get; set; }
        public Quaternion Rotation { get; set; }

        public uint GrabCounter;

        // Non host sends ping on drop for smoothing purposes

        public float LatestRoundTripTime;
        public Vector3 Velocity { get; set; }
        public Vector3 AngularVelocity { get; set; }

        // Host constructors
        public RigidbodySyncableState()
        {
            Position = new();
            Rotation = new();
            GrabCounter = 0;
        }

        public RigidbodySyncableState(float fixedTime, Vector3 position, Quaternion rotation, uint grabCounter)
        {
            FromHost = true;
            FixedTime = fixedTime;
            Position = position;
            Rotation = rotation;
            GrabCounter = grabCounter;
        }

        // Non host constructor
        public RigidbodySyncableState(float fixedTime, Vector3 position, Quaternion rotation, uint grabCounter, float ping, Vector3 velocity, Vector3 angularVelocity)
        {
            FromHost = false;
            FixedTime = fixedTime;
            Position = position;
            Rotation = rotation;
            GrabCounter = grabCounter;
            LatestRoundTripTime = ping;
            Velocity = velocity;
            AngularVelocity = angularVelocity;
        }

        protected override byte[] ConvertToBytes()
        {
            using MemoryStream stream = new();
            using BinaryWriter writer = new(stream);

            writer.Write(FromHost);
            writer.Write(FixedTime);
            WriteVector3(writer, Position);
            WriteQuaternion(writer, Rotation);
            writer.Write(GrabCounter);

            if (!FromHost)
            {
                writer.Write(LatestRoundTripTime);
                WriteVector3(writer, Velocity);
                WriteVector3(writer, AngularVelocity);
            }

            return stream.ToArray();
        }

        protected override void PopulateFromBytes(byte[] data)
        {
            using MemoryStream stream = new(data);
            using BinaryReader reader = new(stream);

            FromHost = reader.ReadBoolean();

            FixedTime = reader.ReadSingle();
            Position = ReadVector3(reader);
            Rotation = ReadQuaternion(reader);
            GrabCounter = reader.ReadUInt32();

            if (!FromHost)
            {
                LatestRoundTripTime = reader.ReadSingle();
                Velocity = ReadVector3(reader);
                AngularVelocity = ReadVector3(reader);
            }
        }

        #region Vector and Quaternion Serialization Utilities
        private void WriteVector3(BinaryWriter writer, Vector3 vector)
        {
            writer.Write(vector.x);
            writer.Write(vector.y);
            writer.Write(vector.z);
        }

        private void WriteQuaternion(BinaryWriter writer, Quaternion quaternion)
        {
            writer.Write(quaternion.x);
            writer.Write(quaternion.y);
            writer.Write(quaternion.z);
            writer.Write(quaternion.w);
        }

        private Vector3 ReadVector3(BinaryReader reader)
        {
            float x = reader.ReadSingle();
            float y = reader.ReadSingle();
            float z = reader.ReadSingle();
            return new Vector3(x, y, z);
        }

        private Quaternion ReadQuaternion(BinaryReader reader)
        {
            float x = reader.ReadSingle();
            float y = reader.ReadSingle();
            float z = reader.ReadSingle();
            float w = reader.ReadSingle();
            return new Quaternion(x, y, z, w);
        }
        #endregion
    }

}
