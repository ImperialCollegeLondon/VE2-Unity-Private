using UnityEngine;
using static VE2.NonCore.Instancing.Internal.InstanceSyncSerializables;
using VE2.Core.Common;
using System.Collections.Generic;

namespace VE2.NonCore.Instancing.Internal
{
    internal class InstantMessageRouter
    {

        private readonly IPluginSyncCommsHandler _commsHandler;
        private Dictionary<string, InstantMessageHandlerService> _instantMessageHandlers;

        // Register/ Deregister methods to add or remove IMHs from Dictionary


        public InstantMessageRouter(IPluginSyncCommsHandler commsHandler)
        {
            _commsHandler = commsHandler;
            _instantMessageHandlers = new();
        }

        public void RegisterInstantMessageHandler(string id, InstantMessageHandlerService instantMessageHandlerService)
        {
            if (!_instantMessageHandlers.ContainsKey(id))
            {
                _instantMessageHandlers.Add(id, instantMessageHandlerService);
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

        public void ReceiveInstantMessage(string id, object message)
        {
            if (_instantMessageHandlers.ContainsKey(id))
            {
                _instantMessageHandlers[id].ReceiveInstantMessage(message);
            }
            else
            {
                Debug.Log($"Received an instant message from handler {id}, but this id isn't registered locally!");
            }
        }

    }
}