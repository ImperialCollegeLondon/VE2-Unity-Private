using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;
using UnityEngine.Events;
using VE2.NonCore.Instancing.API;

namespace VE2.NonCore.Instancing.Internal
{
    [Serializable]
    internal class InstantMessageHandlerConfig
    {
        public void OpenDocs() => Application.OpenURL("https://www.notion.so/InstantMessageHandlers-20f0e4d8ed4d81198659d199062b0181?source=copy_link");
        [EditorButton(nameof(OpenDocs), "Open Docs", PositionType = ButtonPositionType.Above)]
        [SerializeField] public UnityEvent<object> OnMessageReceived = new();

        /// <summary>
        /// If ticked, OnMessageReceived will not be invoked locally when sending a message.
        /// </summary>
        [SerializeField] public bool NoLocalCallback = false;
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
            if (!_config.NoLocalCallback)
                InvokeCustomerEvent(messageObject);
        }

        public void ReceiveInstantMessage(MemoryStream serializedMessageObject)
        {
            // Deserializing here because there's no state module for the Instant Message Handler
            BinaryFormatter binaryFormatter = new();
            object deserializedMessageObject = binaryFormatter.Deserialize(serializedMessageObject);

            InvokeCustomerEvent(deserializedMessageObject);
        }

        private void InvokeCustomerEvent(object obj)
        {
            try
            {
                _config.OnMessageReceived?.Invoke(obj);
            }
            catch (Exception e)
            {
                Debug.LogError($"Error when invoking OnMessageReceived for InstantMessageHandler with ID {_id} \n{e.Message}\n{e.StackTrace}");
            }
        }

        public void TearDown()
        {
            _instanceServiceInternal.DeregisterInstantMessageHandler(_id);
        }

    }
}