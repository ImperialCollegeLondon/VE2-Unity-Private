using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;
using VE2.Common;
using VE2.Core.Common;
using VE2.NonCore.Instancing.VComponents.NonInteractableInterfaces;
using static VE2.Common.CommonSerializables;

namespace VE2.NonCore.Instancing.VComponents.Internal
{
    [Serializable]
    public class RigidbodySyncableStateConfig : BaseStateConfig { }

    internal class RigidbodySyncableStateModule : BaseWorldStateModule, IRigidbodySyncableStateModule
    {
        /// <value>
        /// <see cref="float"/>: Fixed time sent, <see cref="Vector3"/>: Position, <see cref="Quaternion"/>: Rotation
        /// </value>
        public UnityEvent<float, Vector3, Quaternion> OnReceiveState = new();
        private RigidbodySyncableState _state => (RigidbodySyncableState)State;
        private RigidbodySyncableStateConfig _config => (RigidbodySyncableStateConfig)Config;

        public bool MultiplayerSupportPresent => _config.MultiplayerSupportPresent;
        public bool IsHost => _config.MultiplayerSupport.IsHost;

        public RigidbodySyncableStateModule(VE2Serializable state, BaseStateConfig config, string id, WorldStateModulesContainer worldStateModulesContainer) : base(state, config, id, worldStateModulesContainer) { }


        protected override void UpdateBytes(byte[] newBytes)
        {
            _state.Bytes = newBytes;
            try
            {
                OnReceiveState?.Invoke(_state.FixedTime, _state.Position, _state.Rotation);
            }
            catch (Exception e)
            {
                Debug.Log($"Error when emitting OnReceiveState from RigidbodySyncable with ID {ID} \n{e.Message}\n{e.StackTrace}");
            }
        }

        public void SetState(float fixedTime, Vector3 position, Quaternion rotation)
        {
            _state.FixedTime = fixedTime;
            _state.Position = position;
            _state.Rotation = rotation;
        }

    }

    [Serializable]
    public class RigidbodySyncableState : VE2Serializable
    {
        public float FixedTime;
        public Vector3 Position { get; set; }
        public Quaternion Rotation { get; set; }


        public RigidbodySyncableState()
        {
            Position = new();
            Rotation = new();
        }

        public RigidbodySyncableState(float fixedTime, Vector3 position, Quaternion rotation)
        {
            FixedTime = fixedTime;
            Position = position;
            Rotation = rotation;
        }

        protected override byte[] ConvertToBytes()
        {
            using MemoryStream stream = new();
            using BinaryWriter writer = new(stream);

            writer.Write(FixedTime);
            WriteVector3(writer, Position);
            WriteQuaternion(writer, Rotation);

            return stream.ToArray();
        }

        protected override void PopulateFromBytes(byte[] data)
        {
            using MemoryStream stream = new(data);
            using BinaryReader reader = new(stream);

            FixedTime = reader.ReadSingle();
            Position = ReadVector3(reader);
            Rotation = ReadQuaternion(reader);
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
