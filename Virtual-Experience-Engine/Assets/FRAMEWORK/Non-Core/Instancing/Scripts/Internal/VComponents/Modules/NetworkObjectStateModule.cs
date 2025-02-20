using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;
using UnityEngine.Events;
using VE2.Core.VComponents.API;
using VE2.NonCore.Instancing.API;
using static VE2.Core.Common.CommonSerializables;

namespace VE2.NonCore.Instancing.Internal
{
    [Serializable]
    internal class NetworkObjectStateConfig : BaseWorldStateConfig
    {
        [SerializeField] public UnityEvent<object> OnStateChange = new();
    }

    internal class NetworkObjectStateModule : BaseWorldStateModule, INetworkObjectStateModule
    {
        public UnityEvent<object> OnStateChange => _config.OnStateChange;

        public object NetworkObject { get => DeserializedNetworkObject(); set => SerializeNetworkObject(value); }

        private NetworkObjectState _state => (NetworkObjectState)State;
        private NetworkObjectStateConfig _config => (NetworkObjectStateConfig)Config;

        public NetworkObjectStateModule(VE2Serializable state, BaseWorldStateConfig config, string id, IWorldStateSyncService worldStateSyncService) : base(state, config, id, worldStateSyncService) {}

        private void SerializeNetworkObject(object unserializedNetworkObject)
        {
            try
            {
                BinaryFormatter binaryFormatter = new();
                _state.SerializedNetworkObject.SetLength(0);
                binaryFormatter.Serialize(_state.SerializedNetworkObject, unserializedNetworkObject);
            }
            catch (Exception e)
            {
                Debug.Log($"Error encountered when trying to serialize NetworkObject with ID {ID} \n{e.Message}\n{e.StackTrace}");
                return;
            }

            InvokeCustomerOnStateChangeEvent();
        }

        private object DeserializedNetworkObject()
        {
            BinaryFormatter binaryFormatter = new();
            object deserializedNetworkObject = binaryFormatter.Deserialize(_state.SerializedNetworkObject);

            return deserializedNetworkObject;
        }

        private void InvokeCustomerOnStateChangeEvent()
        {
            try
            {
                _config.OnStateChange?.Invoke(NetworkObject);
            }
            catch (Exception e)
            {
                Debug.Log($"Error when emitting OnStateChange from NetworkObject with ID {ID} \n{e.Message}\n{e.StackTrace}");
            }
        }

        protected override void UpdateBytes(byte[] newBytes)
        {
            State.Bytes = newBytes;
            InvokeCustomerOnStateChangeEvent();
        }
    }

    [Serializable]
    public class NetworkObjectState : VE2Serializable
    {
        public ushort StateChangeNumber { get; set; }
        private MemoryStream _serializedNetworkObject;
        public MemoryStream SerializedNetworkObject { 
            get 
            {
                _serializedNetworkObject.Position = 0;
                return _serializedNetworkObject;
            } 
            set => _serializedNetworkObject = value; 
        }

        public NetworkObjectState()
        {
            StateChangeNumber = 0;
            _serializedNetworkObject = new();
        }

        protected override byte[] ConvertToBytes()
        {
            using MemoryStream stream = new();
            using BinaryWriter writer = new(stream);

            writer.Write(StateChangeNumber);

            writer.Write((int)SerializedNetworkObject.Length);

            if (SerializedNetworkObject.Length > 0)
                SerializedNetworkObject.CopyTo(stream);

            return stream.ToArray();
        }

        protected override void PopulateFromBytes(byte[] data)
        {
            using MemoryStream stream = new(data);
            using BinaryReader reader = new(stream);

            StateChangeNumber = reader.ReadUInt16();

            int bytesLength = reader.ReadInt32();

            SerializedNetworkObject.SetLength(0);
            byte[] networkObjectBytes = reader.ReadBytes(bytesLength);
            if (bytesLength > 0)
                SerializedNetworkObject.Write(networkObjectBytes, 0, networkObjectBytes.Length);
        }
    }
}
