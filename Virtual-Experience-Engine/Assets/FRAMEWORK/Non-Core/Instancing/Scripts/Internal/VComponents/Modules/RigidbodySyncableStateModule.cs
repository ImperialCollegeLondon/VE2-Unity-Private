using System;
using System.IO;
using UnityEngine;
using UnityEngine.Events;
using VE2.Core.VComponents.API;
using VE2.NonCore.Instancing.API;
using static VE2.Core.Common.CommonSerializables;

namespace VE2.NonCore.Instancing.Internal
{
    [Serializable]
    internal class RigidbodySyncableStateConfig : BaseWorldStateConfig {

        public bool LogDebugMessages = false;
    }

    internal class RigidbodySyncableStateModule : BaseWorldStateModule, IRigidbodySyncableStateModule
    {
        /// <value>
        /// <see cref="float"/>: Fixed time sent, <see cref="Vector3"/>: Position, <see cref="Quaternion"/>: Rotation
        /// </value>
        public UnityEvent<float, Vector3, Quaternion> OnReceiveState = new();
        private RigidbodySyncableState _state => (RigidbodySyncableState)State;
        private RigidbodySyncableStateConfig _config => (RigidbodySyncableStateConfig)Config;

        private readonly IInstanceService _instanceService;
        public bool IsHost => _instanceService.IsHost;

        public Action<ushort> OnHostChanged;

        /*
            This needs to know host vs non-host - the other VCs don't need this 
            we DO still need to inject WorldStateSyncService so that we can register with the syncer 
            THEN, we could ALSO inject the InstancingAPI, or we could just add host endpoints to the WorldStateSyncService
        */


        public RigidbodySyncableStateModule(VE2Serializable state, BaseWorldStateConfig config, string id, IWorldStateSyncService worldStateSyncService, IInstanceService instanceService) : base(state, config, id, worldStateSyncService) 
        {
            _instanceService = instanceService;
            _instanceService.OnHostChanged += HandleHostChange;
        }


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

        public void HandleHostChange(ushort hostID)
        {
            OnHostChanged?.Invoke(hostID);
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
