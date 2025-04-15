using System;
using UnityEngine;
using UnityEngine.Events;

namespace VE2.NonCore.Instancing.Internal
{
    [Serializable]
    internal class InstantMessageHandlerConfig
    {
        [SerializeField] public UnityEvent<object> OnMessageReceived = new();
    }

    internal class InstantMessageHandlerService
    {
        private readonly string _id;
        private readonly IInstanceServiceInternal _instanceServiceInternal;

        private InstantMessageHandlerConfig _config;

        public InstantMessageHandlerService(InstantMessageHandlerConfig config, string id, IInstanceServiceInternal instanceServiceInternal)
        {
            _id = id;
            _instanceServiceInternal = instanceServiceInternal;
            _config = config;
        }


        public void SendInstantMessage(object messageObject)
        {
            _instanceServiceInternal.SendInstantMessage(_id, messageObject);
        }

        public void ReceiveInstantMessage(object messageObject)
        {

            _config.OnMessageReceived?.Invoke(messageObject);
        }
    }
}