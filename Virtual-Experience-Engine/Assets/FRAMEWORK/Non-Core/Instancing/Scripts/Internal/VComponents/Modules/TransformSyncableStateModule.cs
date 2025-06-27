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
    internal class TransformSyncableStateConfig : WorldStateSyncConfig { }

    internal class TransformSyncableStateModule : BaseWorldStateModule, ITransformSyncableStateModule
    {
        public UnityEvent<ITransformWrapper> OnReceiveState = new();
        private TransformSyncableState _state => (TransformSyncableState)State;

        public TransformSyncableStateModule(ITransformWrapper transformWrapper, WorldStateSyncConfig config, string id, IWorldStateSyncableContainer worldStateModulesContainer) :
            base(new TransformSyncableState(transformWrapper), config, id, worldStateModulesContainer) { }
        protected override void UpdateBytes(byte[] newBytes)
        {
            _state.Bytes = newBytes;

            try
            {
                OnReceiveState?.Invoke(_state.TransformWrapper);
            }
            catch (Exception e)
            {
                Debug.LogError($"Error when emitting OnReceiveState from TransformSyncable with ID {ID} \n{e.Message}\n{e.StackTrace}");
            }
        }
        
        public void SetStateFromHost(ITransformWrapper transformWrapper)
        {
            _state.TransformWrapper.position = transformWrapper.position;
            _state.TransformWrapper.rotation = transformWrapper.rotation;
            _state.TransformWrapper.scale = transformWrapper.scale;
        }
    }

    [Serializable]
    internal class TransformSyncableState : VE2Serializable
    {
        public ITransformWrapper TransformWrapper;

        public TransformSyncableState(ITransformWrapper transformWrapper)
        {
            TransformWrapper = transformWrapper;
        }

        protected override byte[] ConvertToBytes()
        {
            using MemoryStream stream = new MemoryStream();
            using BinaryWriter writer = new BinaryWriter(stream);

            WriteVector3(writer, TransformWrapper.position);
            WriteQuaternion(writer, TransformWrapper.rotation);
            WriteVector3(writer, TransformWrapper.scale);

            return stream.ToArray();
        }

        protected override void PopulateFromBytes(byte[] bytes)
        {
            using MemoryStream stream = new MemoryStream(bytes);
            using BinaryReader reader = new BinaryReader(stream);

            TransformWrapper.position = ReadVector3(reader);
            TransformWrapper.rotation = ReadQuaternion(reader);
            TransformWrapper.scale = ReadVector3(reader);
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
