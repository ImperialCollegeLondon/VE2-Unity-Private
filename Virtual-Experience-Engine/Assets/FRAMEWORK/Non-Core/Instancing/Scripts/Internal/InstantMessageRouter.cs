using UnityEngine;
using static VE2.NonCore.Instancing.Internal.InstanceSyncSerializables;
using VE2.Core.Common;

namespace VE2.NonCore.Instancing.Internal
{
    internal class InstantMessageRouter
    {

        private readonly IPluginSyncCommsHandler _commsHandler;
        // Dictionary of IMHs in scene - ids vs reference to send back to
        // Register/ Deregister methods to add or remove IMHs from Dictionary


        public InstantMessageRouter(IPluginSyncCommsHandler commsHandler)
        {
            _commsHandler = commsHandler;
        }


        public void SendInstantMessage(string id, object message)
        {
            // Attempt to serialize message
            InstantMessage instantMessage = new(id, message);
            _commsHandler.SendMessage(instantMessage.Bytes, InstanceNetworkingMessageCodes.InstantMessage, TransmissionProtocol.TCP);
        }

    }
}