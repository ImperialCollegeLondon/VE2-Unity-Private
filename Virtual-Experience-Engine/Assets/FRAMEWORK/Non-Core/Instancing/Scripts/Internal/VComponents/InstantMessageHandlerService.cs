using UnityEngine;

namespace VE2.NonCore.Instancing.Internal
{
    internal class InstantMessageHandlerService
    {
        private readonly string _id;
        private readonly IInstanceServiceInternal _instanceServiceInternal;

        public InstantMessageHandlerService(string id, IInstanceServiceInternal instanceServiceInternal)
        {
            _id = id;
            _instanceServiceInternal = instanceServiceInternal;
        }


        public void SendInstantMessage(object messageObject)
        {
            _instanceServiceInternal.SendInstantMessage(_id, messageObject);
        }

        public void ReceiveInstantMessage(object messageObject)
        {

        }
    }
}