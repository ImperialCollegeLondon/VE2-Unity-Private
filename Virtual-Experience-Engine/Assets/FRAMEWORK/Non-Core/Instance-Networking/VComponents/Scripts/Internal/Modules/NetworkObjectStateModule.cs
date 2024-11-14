using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;
using UnityEngine.Events;
using VE2.Common;
using VE2.Core.Common;
using VE2.NonCore.Instancing.VComponents.NonInteractableInterfaces;
using static VE2.Common.CommonSerializables;

namespace VE2.NonCore.Instancing.VComponents.Internal
{
    [Serializable]
    public class NetworkObjectStateConfig : BaseStateConfig
    {
        [SerializeField] public UnityEvent<object> OnStateChange = new();
    }

    internal class NetworkObjectStateModule : BaseWorldStateModule, INetworkObjectStateModule
    {
        public UnityEvent<object> OnStateChange => _config.OnStateChange;

        private MemoryStream _serializedNetworkObject = new();
        public object NetworkObject { get => GetUnserializedNetworkObject(); set => HandleExternalStateChange(value); }

        private NetworkObjectState _state => (NetworkObjectState)State;
        private NetworkObjectStateConfig _config => (NetworkObjectStateConfig)Config;


        public NetworkObjectStateModule(VE2Serializable state, BaseStateConfig config, string id, WorldStateModulesContainer worldStateModulesContainer) : base(state, config, id, worldStateModulesContainer) { }

        private void HandleExternalStateChange(object unserializedNetworkObject)
        {
            try
            {
                BinaryFormatter binaryFormatter = new();
                binaryFormatter.Serialize(_serializedNetworkObject, unserializedNetworkObject);

                _state.SerializedNetworkObject = _serializedNetworkObject;
            }
            catch (Exception e)
            {
                Debug.Log($"Error encountered when trying to serialize NetworkObject with ID {ID} \n{e.Message}\n{e.StackTrace}");
                return;
            }

            InvokeCustomerOnStateChangeEvent();
        }

        private object GetUnserializedNetworkObject()
        {
            BinaryFormatter binaryFormatter = new();
            object deserializedNetworkObject = binaryFormatter.Deserialize(_serializedNetworkObject);

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
        public MemoryStream SerializedNetworkObject { get; set; }

        public NetworkObjectState()
        {
            StateChangeNumber = 0;
            SerializedNetworkObject = null;
        }

        protected override byte[] ConvertToBytes()
        {
            using MemoryStream stream = new();
            using BinaryWriter writer = new(stream);

            writer.Write(StateChangeNumber);

            if (SerializedNetworkObject != null)
            {
                byte[] networkObjectBytes = SerializedNetworkObject.ToArray();

                writer.Write(networkObjectBytes.Length);
                writer.Write(networkObjectBytes);
            }
            else
            {
                writer.Write(-1);
            }

            return stream.ToArray();
        }

        protected override void PopulateFromBytes(byte[] data)
        {
            using MemoryStream stream = new(data);
            using BinaryReader reader = new(stream);

            StateChangeNumber = reader.ReadUInt16();
            int bytesLength = reader.ReadInt32();

            if (bytesLength != -1)
            {
                byte[] networkObjectBytes = reader.ReadBytes(bytesLength);

                BinaryFormatter binaryFormatter = new BinaryFormatter();
                MemoryStream SerializedNetworkObject = new(networkObjectBytes);

                Debug.Log($"Successfully deserialized {binaryFormatter.Deserialize(SerializedNetworkObject)}");
            }
            else
            {
                SerializedNetworkObject = null;
            }
        }
    }
}
