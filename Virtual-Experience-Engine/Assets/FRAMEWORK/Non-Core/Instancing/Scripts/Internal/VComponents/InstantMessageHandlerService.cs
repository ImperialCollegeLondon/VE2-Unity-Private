using System;
using UnityEngine;
using UnityEngine.Events;
using VE2.NonCore.Instancing.API;

namespace VE2.NonCore.Instancing.Internal
{
    [Serializable]
    internal class InstantMessageHandlerConfig
    {
        [SerializeField] public UnityEvent<object> OnMessageReceived = new();
    }

    internal class InstantMessageHandlerService : IInstantMessageHandlerInternal
    {
        private readonly string _id;
        private readonly IInstanceServiceInternal _instanceServiceInternal;

        private InstantMessageHandlerConfig _config;

        public InstantMessageHandlerService(InstantMessageHandlerConfig config, string id, IInstanceServiceInternal instanceServiceInternal)
        {
            _id = id;
            _instanceServiceInternal = instanceServiceInternal;
            _config = config;
            _instanceServiceInternal.RegisterInstantMessageHandler(_id, this);
        }


        public void SendInstantMessage(object messageObject)
        {
            _instanceServiceInternal.SendInstantMessage(_id, messageObject);
            // Instance server won't send IM back to sender, but it can still useful to trigger this event locally
            _config.OnMessageReceived?.Invoke(messageObject);
        }

        public void ReceiveInstantMessage(object messageObject)
        {

            _config.OnMessageReceived?.Invoke(messageObject);
        }

        public void TearDown()
        {
            _instanceServiceInternal.DeregisterInstantMessageHandler(_id);
        }

    }
}