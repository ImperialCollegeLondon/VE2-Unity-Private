using UnityEngine;
using static VE2.NonCore.Instancing.Internal.InstanceSyncSerializables;
using System.Collections.Generic;
using VE2.NonCore.Instancing.API;
using VE2.Common.Shared;

namespace VE2.NonCore.Instancing.Internal
{
    internal class InstantMessageRouter
    {

        private readonly IPluginSyncCommsHandler _commsHandler;
        private Dictionary<string, IInstantMessageHandlerInternal> _instantMessageHandlers;

        // Register/ Deregister methods to add or remove IMHs from Dictionary


        public InstantMessageRouter(IPluginSyncCommsHandler commsHandler)
        {
            _commsHandler = commsHandler;
            _instantMessageHandlers = new();
            _commsHandler.OnReceiveInstantMessage += ReceiveInstantMessage;
        }

        public void RegisterInstantMessageHandler(string id, IInstantMessageHandlerInternal instantMessageHandler)
        {
            if (!_instantMessageHandlers.ContainsKey(id))
            {
                _instantMessageHandlers.Add(id, instantMessageHandler);
            }
            else
            {
                Debug.Log($"Tried to register instant message handler {id}, but it was already registered!");
            }
            
        }

        public void DeregisterInstantMessageHandler(string id)
        {
            if (_instantMessageHandlers.ContainsKey(id))
            {
                _instantMessageHandlers.Remove(id);
            }
            else
            {
                Debug.Log($"Tried to deregister instant message handler {id}, but it wasn't previously registered!");
            }
            
        }

        public void SendInstantMessage(string id, object message)
        {
            // Attempt to serialize message
            InstantMessage instantMessage = new(id, message);
            _commsHandler.SendMessage(instantMessage.Bytes, InstanceNetworkingMessageCodes.InstantMessage, TransmissionProtocol.TCP);
        }

        public void ReceiveInstantMessage(byte[] bytes)
        {
            InstantMessage instantMessage = new(bytes);

            if (_instantMessageHandlers.ContainsKey(instantMessage.Id))
            {
                _instantMessageHandlers[instantMessage.Id].ReceiveInstantMessage(instantMessage.SerializedMessageObject);
            }
            else
            {
                Debug.Log($"Received an instant message from handler {instantMessage.Id}, but this id isn't registered locally!");
            }
        }



    }
}