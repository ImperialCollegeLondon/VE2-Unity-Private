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
        public object NetworkObject { get => _state.NetworkObject; set => HandleExternalStateChange(value); }

        private NetworkObjectState _state => (NetworkObjectState)State;
        private NetworkObjectStateConfig _config => (NetworkObjectStateConfig)Config;


        public NetworkObjectStateModule(VE2Serializable state, BaseStateConfig config, string id, WorldStateModulesContainer worldStateModulesContainer) : base(state, config, id, worldStateModulesContainer) { }

        private void HandleExternalStateChange(object unserializedNetworkObject)
        {
            _state.NetworkObject = unserializedNetworkObject;
            InvokeCustomerOnStateChangeEvent();
        }


        private void InvokeCustomerOnStateChangeEvent()
        {
            try
            {
                _config.OnStateChange?.Invoke(_state.NetworkObject);
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
        public object NetworkObject { get; set; }

        public NetworkObjectState()
        {
            StateChangeNumber = 0;
            NetworkObject = null;
        }

        protected override byte[] ConvertToBytes()
        {
            using MemoryStream stream = new();
            using BinaryWriter writer = new(stream);

            writer.Write(StateChangeNumber);

            if (NetworkObject != null)
            {
                BinaryFormatter binaryFormatter = new();
                MemoryStream serializedNetworkObject = new();

                binaryFormatter.Serialize(serializedNetworkObject, NetworkObject);
                byte[] networkObjectBytes = serializedNetworkObject.ToArray();

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

                BinaryFormatter binaryFormatter = new();
                MemoryStream serializedNetworkObject = new(networkObjectBytes);

                NetworkObject = binaryFormatter.Deserialize(serializedNetworkObject);
            }
            else
            {
                NetworkObject = null;
            }
        }
    }
}
